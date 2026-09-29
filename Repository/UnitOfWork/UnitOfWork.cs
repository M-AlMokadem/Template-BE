using Domain.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private IDbContextTransaction? _transaction;

        public ApplicationContext Context { get; }

        public UnitOfWork(ApplicationContext context)
        {
            Context = context;
        }

        public async Task Commit()
        {
            await Context.SaveChangesAsync();
        }
        public async Task<int> CommitAsync()
        {
            return await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            Context.Dispose();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await Context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException("No active transaction exists.");
            }

            try
            {
                await Context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException("No active transaction exists.");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
