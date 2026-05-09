using AutoMapper;
using LMS.Application.Auth.Dtos;
using LMS.Domain.Entities;

namespace LMS.Application.Auth.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
