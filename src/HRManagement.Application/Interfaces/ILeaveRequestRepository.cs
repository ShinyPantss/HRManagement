using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(int id);
    Task<IEnumerable<LeaveRequest>> GetAllAsync();
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<int> AddAsync(LeaveRequest leaveRequest);
    Task UpdateAsync(LeaveRequest leaveRequest);
    Task DeleteAsync(int id);

    // Silme öncesi bağımlılık kontrolü.
    Task<bool> ExistsByEmployeeIdAsync(int employeeId);
}           