using AutoMapper;
using LMS.Application.Auth.Dtos;
using LMS.Domain.Entities;

namespace LMS.Application.Auth.Mappings;

// Fully-qualified: the LMS.Application.Profile feature namespace would
// otherwise shadow AutoMapper's Profile base type here.
public class UserMappingProfile : AutoMapper.Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
