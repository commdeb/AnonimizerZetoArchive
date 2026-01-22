using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataLayer
{
    public abstract class UnitOfWork : IUnitOfWork
    {
        public abstract ICommand Command { get; set; }

        protected UnitOfWork(ICommand command) 
        {
            Command = command;
        }
        public abstract void CommitTransaction();
        public abstract void Dispose();
        public abstract void BeginTransaction();
        public abstract void RollbackTransaction();
        public abstract void ExecuteCommand();
        public abstract Task ExecuteCommandAsync();
        public abstract DT ExecuteCommand<DT>() where DT : DataTable;
        public abstract Task<DT> ExecuteCommandAsync<DT>() where DT : DataTable; 

    }

    public abstract class DTO<DT,DR>(DT innerTable) where DT : DataTable where DR : DataRow
    {
        private readonly DT _innerTable = innerTable;

        public abstract void ConvertFrom(DR row); 
        public abstract DR ConvertTo();

        public abstract List<DTO<DT, DR>> ConvertFromTable(DT table);
        public abstract DT ConvertToTable(List<DTO<DT, DR>> list);

    }
}
