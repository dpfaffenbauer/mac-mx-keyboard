namespace Loupedeck.MacKeysPlugin
{
    using System;

    // Not tied to an application (HasNoApplication); the service expects the class anyway.
    public class MacKeysApplication : ClientApplication
    {
        public MacKeysApplication()
        {
        }

        protected override String GetProcessName() => "";

        protected override String GetBundleName() => "";

        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
