﻿using BankAccount.Commands;
using BankAccount.Infrastructure.Buses;
using BankAccount.Infrastructure.Commanding;
using BankAccount.Infrastructure.Storage;
using BankAccount.ProcessManager.Base;

namespace BankAccount.ProcessManager
{
    public class CreateBankAccountSaga : Saga,
        IAmStartedBy<CreateCustomerCommand>
    {
        #region C-Tor

        public CreateBankAccountSaga(
            ISagaBus bus, 
            ICommandStackRepository<Domain.CustomerDomainModel> repository) 
            : base(bus, repository)
        {
        }

        #endregion

        #region Handling commands

        public void Handle(CreateCustomerCommand message)
        {
            var aggregate = Domain.CustomerDomainModel.Factory.CreateNewInstance(
                message.Id,
                message.Version,
                message.Person.FirstName,
                message.Person.LastName,
                message.Person.IdCard,
                message.Person.IdNumber,
                message.Person.Dob,
                message.Contact.Email,
                message.Contact.PhoneNumber,
                //message.Money.Balance,
                //message.Money.Currency,
                message.Address.Street,
                message.Address.Zip,
                message.Address.Hausnumber,
                message.Address.City,
                message.Address.State);

            this.Repository.Save(aggregate, aggregate.Version);
        }

        #endregion
    }
}
