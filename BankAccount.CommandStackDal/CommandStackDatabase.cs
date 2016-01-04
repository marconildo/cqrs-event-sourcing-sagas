﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using BankAccount.CommandStackDal.Abstraction;
using BankAccount.CommandStackDal.Exceptions;
using BankAccount.DbModel.Entities;
using BankAccount.DbModel.ItemDb;
using BankAccount.Domain;

namespace BankAccount.CommandStackDal
{
    public sealed class CommandStackDatabase : ICommandStackDatabase
    {
        //private static readonly List<CustomerDomainModel> Cache = new List<CustomerDomainModel>();

        #region ICommandStackDatabase implementation

        public void Save(CustomerDomainModel item)
        {
            CustomerEntity entity;
            using (var ctx = new BankAccountDbContext())
            {
                entity = ctx.CustomerSet.SingleOrDefault(b => b.AggregateId == item.Id);
            }

            if (entity == null)
            {
                this.AddCustomer(item);
            }
            else
            {
                this.UpdateCustomer(item);
            }
        }

        public void Save(AccountDomainModel item)
        {
            AccountEntity entity;
            using (var ctx = new BankAccountDbContext())
            {
                entity = ctx.AccountSet.SingleOrDefault(a => a.AggregateId == item.Id);
            }

            if (entity == null)
            {
                this.AddAccount(item);
            }
            else
            {
                this.UpdateAccount(item);
            }
        }

        //public void DeleteCustomer(Guid id)
        //{
        //    using (var ctx = new BankAccountDbContext())
        //    {
        //        var entity = ctx.CustomerSet.SingleOrDefault(b => b.AggregateId == id);
        //        if (entity == null)
        //        {
        //            throw new AggregateNotFoundException($"Aggregate with the id {id} was not found");
        //        }
        //        entity.CustomerState = State.Closed;

        //        ctx.Entry(entity).State = EntityState.Modified;
        //        ctx.SaveChanges();
        //    }
        //}

        //public void DeleteAccount(Guid id)
        //{
        //    using (var ctx = new BankAccountDbContext())
        //    {
        //        var entity = ctx.AccountSet.SingleOrDefault(b => b.AggregateId == id);
        //        if (entity == null)
        //        {
        //            throw new AggregateNotFoundException($"Aggregate with the id {id} was not found");
        //        }
        //        entity.AccountState = State.Closed;

        //        ctx.Entry(entity).State = EntityState.Modified;
        //        ctx.SaveChanges();
        //    }
        //}

        //public void AddToCache(CustomerDomainModel ba)
        //{
        //    var acc = Cache.SingleOrDefault(b => b.Id == ba.Id);
        //    if (acc == null)
        //    {
        //        Cache.Add(ba);
        //    }
        //    else
        //    {
        //        Cache.Remove(acc);
        //        Cache.Add(ba);
        //    }
        //}

        //public void UpdateFromCache()
        //{
        //    if (!Cache.Any())
        //        return;

        //    foreach (var entity in Cache)
        //    {
        //        this.UpdateCustomer(entity);
        //    }

        //    Cache.Clear();
        //}

        #endregion

        #region Helpers

        private void AddCustomer(CustomerDomainModel item)
        {
            using (var ctx = new BankAccountDbContext())
            {
                ctx.CustomerSet.Add(new CustomerEntity
                {
                    AggregateId         = item.Id,
                    Version             = item.Version,
                    FirstName           = item.Person.FirstName,
                    LastName            = item.Person.LastName,
                    CustomerState       = State.Open
                });
                ctx.SaveChanges();
            }
        }

        private void UpdateCustomer(CustomerDomainModel item)
        {
            using (var ctx = new BankAccountDbContext())
            {
                var entity = ctx.CustomerSet.SingleOrDefault(b => b.AggregateId == item.Id);
                if (entity == null)
                {
                    throw new AggregateNotFoundException("Bank account");
                }

                entity.Version              = item.Version;
                entity.FirstName            = item.Person.FirstName;
                entity.LastName             = item.Person.LastName;
                entity.CustomerState        = ConvertState(item.State);

                ctx.Entry(entity).State     = EntityState.Modified;
                ctx.SaveChanges();
            }
        }

        private State ConvertState(ValueTypes.State state)
        {
            switch (state)
            {
                case ValueTypes.State.Open:
                    return State.Open;
                case ValueTypes.State.Closed:
                    return State.Closed;
                case ValueTypes.State.Locked:
                    return State.Locked;
                default:
                    return State.Unlocked;
            }
        }

        private void AddAccount(AccountDomainModel item)
        {
            using (var ctx = new BankAccountDbContext())
            {
                var customerEntityId =
                    ctx.CustomerSet.SingleOrDefault(c => c.AggregateId == item.CustomerId);
                if (customerEntityId == null)
                {
                    throw new AggregateNotFoundException("Bank account");
                }

                ctx.AccountSet.Add(new AccountEntity
                {
                    AggregateId             = item.Id,
                    Version                 = item.Version,
                    CustomerEntityId        = customerEntityId.CustomerEntityId,
                    CustomerAggregateId     = item.CustomerId,
                    Currency                = item.Currency,
                    AccountState            = State.Open
                });
                ctx.SaveChanges();
            }
        }

        private void UpdateAccount(AccountDomainModel item)
        {
            using (var ctx = new BankAccountDbContext())
            {
                var entity = ctx.AccountSet.SingleOrDefault(b => b.AggregateId == item.Id);
                if (entity == null)
                {
                    throw new AggregateNotFoundException("account");
                }

                var customerEntityId =
                    ctx.CustomerSet.SingleOrDefault(c => c.AggregateId == item.CustomerId);
                if (customerEntityId == null)
                {
                    throw new AggregateNotFoundException("Bank account");
                }

                entity.Version = item.Version;
                entity.Currency = item.Currency;
                entity.CustomerEntityId = customerEntityId.CustomerEntityId;
                entity.CustomerAggregateId = item.CustomerId;
                entity.AccountState = ConvertState(item.State);

                ctx.Entry(entity).State = EntityState.Modified;
                ctx.SaveChanges();
            }
        }

        #endregion
    }
}