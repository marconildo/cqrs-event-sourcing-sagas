using System;
using EventStore;

namespace BankAccount.EventStore
{
    public class AuthorizationPipelineHook : IPipelineHook
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Commit Select(Commit committed)
        {
            // return null if the user isn't authorized to see this commit
            return committed;
        }

        public bool PreCommit(Commit attempt)
        {
            // Can easily do logging or other such activities here
            return true; // true == allow commit to continue, false = stop.
        }

        public void PostCommit(Commit committed)
        {
            // anything to do after the commit has been persisted.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this.Dispose();
        }
    }
}