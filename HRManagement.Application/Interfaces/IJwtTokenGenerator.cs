using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}