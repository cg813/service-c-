using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AIQXCommon.Models;
using AutoMapper;
using Newtonsoft.Json;

namespace AIQXCoreService.Domain.Models
{
    public enum UseCaseStatus
    {
        [EnumMember(Value = "live")]
        Live = 1,

        [EnumMember(Value = "in-evaluation")]
        InEvaluation = 2,

        [EnumMember(Value = "under-validation")]
        UnderValidation = 3,

        [EnumMember(Value = "in-implementation")]
        InImplementation = 4,

        [EnumMember(Value = "declined")]
        Declined = 5,
    }


    [Table("use_cases")]
    public class UseCaseEntity : UpdatedAtModel
    {
        public static ImmutableDictionary<string, UseCaseStatus> StatusesDictionary = Enum.GetValues(typeof(UseCaseStatus))
            .Cast<UseCaseStatus>()
            .ToImmutableDictionary(item => StatusToString(item), item => item);

        public static string StatusToString(UseCaseStatus value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            EnumMemberAttribute attribute
                = Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute))
                    as EnumMemberAttribute;

            return attribute == null ? value.ToString() : attribute.Value;
        }

        public static UseCaseStatus? StatusFromString(string step)
        {
            return StatusesDictionary.GetValueOrDefault(step);
        }

        public static string getUseCaseName(string plantId, string building, string name)
        {
            return plantId + "-H" + building + "-" + name;
        }

        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Image { get; set; }

        [Required]
        public string Building { get; set; }

        public string Line { get; set; }

        public string Position { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedBy { get; set; }

        [Required]
        public UseCaseStatus Status { get; set; }

        [Required]
        public PlantEntity Plant { get; set; }

        public ICollection<AttachmentEntity> Attachments { get; set; }

        public ICollection<UseCaseStepEntity> Steps { get; set; }

        public UseCaseStep? GetLastStepOrNull()
        {
            var step = getCompletedSteps().LastOrDefault();
            if (step == null)
            {
                return null;
            }
            return step.Type;
        }

        public UseCaseStep GetCurrentStep()
        {
            int index = getCompletedSteps().Count;
            if (index >= UseCaseStepEntity.StepsOrder.Count)
            {
                return UseCaseStep.Order;
            }
            return UseCaseStepEntity.StepsOrder[index];
        }

        public UseCaseStep? GetNextStepOrNull()
        {
            int index = getCompletedSteps().Count + 1;
            if (index >= UseCaseStepEntity.StepsOrder.Count)
            {
                return null;
            }
            return UseCaseStepEntity.StepsOrder[index];
        }

        public IList<UseCaseStepEntity> getCompletedSteps()
        {
            return Steps.Where(s => s.CompletedAt != null).ToList();
        }
    }

    public class UseCaseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Building { get; set; }

        public string Line { get; set; }

        public string Position { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string CreatedBy { get; set; }

        public string Status { get; set; }

        public string PlantId { get; set; }

        public List<AttachmentDto> Attachments { get; set; }

        public List<UseCaseStepDto> Steps { get; set; }
    }

    public class CreateUseCaseDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string PlantId { get; set; }

        [Required]
        public string Building { get; set; }

        public string Image { get; set; }

        public string Line { get; set; }

        public string Position { get; set; }

    }

    public class UpdateUseCaseDto
    {
#nullable enable
        public string? Name { get; set; }

        public string? Image { get; set; }

        public string? Building { get; set; }

        public string? Line { get; set; }

        public string? Position { get; set; }

        public string? PlantId { get; set; }
#nullable disable

        public void AssignNullFields(UseCaseEntity entity)
        {
            if (Image == null)
                Image = entity.Image;
            if (Building == null)
                Building = entity.Building;
            if (Line == null)
                Line = entity.Line;
            if (Position == null)
                Position = entity.Position;
            if (PlantId == null)
                PlantId = entity.Plant.Id;

            // Special handling name
            if (Name == null)
            {
                Name = entity.Name;
            }
            else
            {
                Name = UseCaseEntity.getUseCaseName(PlantId, Building, Name);
            }
        }
    }

    public class UseCaseAutoMapperProfile : Profile
    {

        public UseCaseAutoMapperProfile()
        {
            CreateMap<UseCaseEntity, UseCaseDto>()
                .ForMember(
                    dest => dest.PlantId,
                    opt => opt.MapFrom(src => src.Plant.Id)
                ).ForMember(
                    dest => dest.Status,
                    opt => opt.MapFrom(src => UseCaseEntity.StatusToString(src.Status))
                ).ForMember(
                    dest => dest.Steps,
                    opt => opt.MapFrom((src, dest, i, context) => src.Steps
                        .Select(s => context.Mapper.Map<UseCaseStepEntity, UseCaseStepDto>(s)).ToList())
                ).ForMember(
                    dest => dest.Attachments,
                    opt => opt.MapFrom((src, dest, i, context) => src.Attachments
                        .Select(s => context.Mapper.Map<AttachmentEntity, AttachmentDto>(s)).ToList())
                ).ReverseMap();

            CreateMap<CreateUseCaseDto, UseCaseEntity>()
                .ForMember(
                    dest => dest.Name,
                    opt => opt.MapFrom(src => UseCaseEntity.getUseCaseName(src.PlantId, src.Building, src.Name))
                ).ReverseMap();

            CreateMap<UpdateUseCaseDto, UseCaseEntity>()
                .ReverseMap();
        }
    }

    public class UseCaseQueryOptions : PagingOption
    {

#nullable enable
        public string? q { get; set; }
        public string? plantId { get; set; }
#nullable disable
    }
}
