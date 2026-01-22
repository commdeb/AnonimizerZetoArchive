using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataLayer
{
    public interface IUnitOfWork : IDisposable
    {
        ICommand Command { get; set; }

        void BeginTransaction();
        void CommitTransaction();

        void RollbackTransaction();
        void ExecuteCommand();
        Task ExecuteCommandAsync();
        DT ExecuteCommand<DT>() where DT : DataTable;
        Task<DT> ExecuteCommandAsync<DT>() where DT : DataTable;    
        
    }

    public interface ICommand { }
    public interface ICommand<DT> : ICommand where DT : DataTable { }
}
