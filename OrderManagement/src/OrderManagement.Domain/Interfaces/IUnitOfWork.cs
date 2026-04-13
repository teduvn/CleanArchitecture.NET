using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
