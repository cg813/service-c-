using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIQXCommon.Middlewares;
using AIQXCommon.Models;
using AIQXCommon.Services;
using AIQXCoreService.Domain.Models;
using AIQXCoreService.Implementation.Persistence;
using Fop;
using Fop.FopExpression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opw.HttpExceptions;

namespace AIQXCoreService.Implementation.Services
{
    public class UseCaseService
    {
        private readonly ILogger<UseCaseService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly ConfigService _config;
        private readonly NotificationService _notification;
        private readonly FileService _fileService;
        private readonly UserService _userService;

        public UseCaseService(ILogger<UseCaseService> logger, AppDbContext context, ConfigService config, NotificationService notification)
        {
            _logger = logger;
            _dbContext = context;
            _config = config;
            _notification = notification;
            _fileService = new FileService(config.FileServiceUrl(), "core-service");
            _userService = new UserService("");
        }

        public async Task<IList<UseCaseEntity>> GetAsync()
        {
            return await _dbContext.UseCases
                .Include(c => c.Plant)
                .Include(c => c.Attachments)
                .Include(c => c.Steps)
                .ToListAsync();
        }

        public async Task<(IList<UseCaseEntity>, PagingResult)> GetByPagingAsync(UseCaseQueryOptions pagingOption)
        {
            string filter = !string.IsNullOrEmpty(pagingOption.q) ? "name~=" + pagingOption.q : null;
            int limit = Math.Min(100, pagingOption.limit ?? 10);
            int page = pagingOption.page ?? 1;
            var fopRequest = FopExpressionBuilder<UseCaseEntity>.Build(filter, pagingOption.order, page, limit);
            var (filteredUseCases, totalCount) = _dbContext.UseCases
                .Include(c => c.Plant)
                .Include(c => c.Attachments)
                .Include(c => c.Steps)
                .Where(c => !string.IsNullOrEmpty(pagingOption.plantId) ? c.Plant.Id == pagingOption.plantId : true)
                .ApplyFop(fopRequest);

            var pagingResult = new PagingResult()
            {
                count = filteredUseCases.Count(),
                page = page,
                pageCount = Math.Max((int)Math.Ceiling(totalCount / (double)limit), 1),
                total = totalCount,
            };
            return (await filteredUseCases.ToListAsync(), pagingResult);
        }

        public async Task<UseCaseEntity> GetByIdAsync(Guid id)
        {
            UseCaseEntity useCase = await _dbContext.UseCases
                .Where(c => c.Id == id)
                .Include(c => c.Plant)
                .Include(c => c.Attachments)
                .Include(c => c.Steps)
                .FirstOrDefaultAsync();

            if (useCase == null)
            {
                throw new NotFoundException("UseCase not found");
            }

            return useCase;
        }

        public async Task<UseCaseEntity> CreateAsync(UseCaseEntity useCase, string plantId, string createdBy)
        {
            PlantEntity plant = await _dbContext.Plants
                .Where(p => p.Id == plantId)
                .FirstOrDefaultAsync();
            if (plant == null)
            {
                throw new NotFoundException("Plant not found");
            }

            useCase.Plant = plant;
            useCase.CreatedBy = createdBy;
            useCase.Status = UseCaseStatus.InEvaluation;
            useCase.UpdatedAt = DateTime.UtcNow;
            useCase.Steps = new List<UseCaseStepEntity>();
            var dbEntity = await _dbContext.UseCases.AddAsync(useCase);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(dbEntity.Entity.Id);
        }

        public async Task<UseCaseEntity> UpdateAsync(Guid id, UpdateUseCaseDto dto)
        {
            UseCaseEntity useCase = await GetByIdAsync(id);
            dto.AssignNullFields(useCase);
            var entry = _dbContext.Entry(useCase);
            entry.CurrentValues.SetValues(dto);
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(entry.Entity.Id);
        }

        public async Task<UseCaseEntity> CompleteStepAsync(Guid id, UseCaseStep step, AuthInformation authInfo)
        {
            UseCaseEntity useCase = await GetByIdAsync(id);
            UseCaseStepEntity useCaseStep = useCase.Steps.FirstOrDefault(item => item.Type == step);
            if (useCaseStep == null)
            {
                throw new ForbiddenException("Step was not created yet.");
            }

            var completedSteps = useCase.Steps.Where(step => step.CompletedAt != null).ToList();
            bool containsStep = completedSteps.Any(item => item.Type == step);
            if (containsStep)
            {
                throw new ForbiddenException("Step has already been completed.");
            }

            int nextStepIndex = UseCaseStepEntity.StepsOrder.IndexOf(step);
            if (nextStepIndex < 0)
            {
                throw new BadRequestException($"Unknown '{step}'.");
            }

            int currStepIndex = completedSteps.Count - 1;
            if (nextStepIndex - currStepIndex != 1)
            {
                throw new ForbiddenException($"Step '{UseCaseStepEntity.StepsOrder[currStepIndex]}' needs to be completed first.");
            }

            // This completes the step.
            useCaseStep.CompletedAt = DateTime.UtcNow;
            useCase.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            bool isAiqxStep = UseCaseStepEntity.IsAiqxStep(step);

            if (nextStepIndex == UseCaseStepEntity.StepsOrder.Count - 1)
            {
                await SetStatusAsync(id, UseCaseStatus.InImplementation);
            }
            else
            {
                await SetStatusAsync(id, isAiqxStep ? UseCaseStatus.InEvaluation : UseCaseStatus.UnderValidation);
            }

            string[] recipients;
            string lang = "en";
            if (isAiqxStep)
            {
                var user = await _userService.getUserByIdAsync(useCase.CreatedBy);
                recipients = new string[] { user.Mail };
                lang = user.Language;
            }
            else
            {
                recipients = _config.SmtpRecipientAddresses();
            }

            // TODO: Use save join mechanism.
            var formUrl = $"{_config.FrontendUseCaseDetailURL()}/{useCase.Id.ToString()}";
            // TODO: Get user lang.
            _notification.sendHtmlEmail(recipients, useCase.Name, lang, formUrl, step);

            // Lock files linked to the attachments.
            var entry = AttachmentEntity.StepsDictionary.FirstOrDefault(s => s.Value == step);
            if (!entry.Equals(default(KeyValuePair<AttachmentType, UseCaseStep>)))
            {
                var tasks = useCase.Attachments
                    .Where(a => a.Type == AttachmentEntity.TypeToString(entry.Key))
                    .Select(a => _fileService.LockFile(a.RefId, authInfo))
                    .ToArray();
                await Task.WhenAll(tasks);
            }

            return await GetByIdAsync(id);
        }

        public async Task<UseCaseEntity> UpdateStepAsync(Guid id, UseCaseStep step, string createdBy, Object dto)
        {
            UseCaseEntity useCase = await GetByIdAsync(id);
            UseCaseStepEntity useCaseStep = useCase.Steps.FirstOrDefault(item => item.Type == step);
            if (useCaseStep == null)
            {
                // Step was not submitted before, create a new one.
                useCaseStep = new UseCaseStepEntity()
                {
                    UseCase = useCase,
                    Type = step,
                    CreatedBy = createdBy,
                };
            }
            useCaseStep.Form = JsonConvert.SerializeObject(dto);

            UseCaseEntity entity = _dbContext.Entry(useCase).Entity;

            var updatedSteps = useCase.Steps.Where(item => item.Type != step).ToList();
            updatedSteps.Add(useCaseStep);
            entity.Steps = updatedSteps;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<UseCaseEntity> SetStatusAsync(Guid id, UseCaseStatus status)
        {
            UseCaseEntity useCase = await GetByIdAsync(id);
            UseCaseEntity entity = _dbContext.Entry(useCase).Entity;
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            UseCaseEntity useCase = await GetByIdAsync(id);
            _dbContext.UseCases.Remove(useCase);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private bool IsAiqxStep(UseCaseStep step)
        {
            return UseCaseStepEntity.StepsOrder.IndexOf(step) % 2 == 1;
        }
    }
}
