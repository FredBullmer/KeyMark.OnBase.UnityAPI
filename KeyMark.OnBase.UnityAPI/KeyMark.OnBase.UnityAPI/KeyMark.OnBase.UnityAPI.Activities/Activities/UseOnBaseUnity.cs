using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Statements;
using System.ComponentModel;
using KeyMark.OnBase.UnityAPI.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;


namespace KeyMark.OnBase.UnityAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_DisplayName))]
    [LocalizedDescription(nameof(Resources.UseOnBaseUnity_Description))]
    public class UseOnBaseUnity : ContinuableAsyncNativeActivity
    {
        #region Properties
        Application obApp = null;
        string msg = string.Empty;
        bool keepConnected = false;

        public enum LicTypes
        {
            Default, 
            EnterpriseCore, 
            QueryMetering
        }

        [Browsable(false)]
        public ActivityAction<IObjectContainer​> Body { get; set; }

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_AppServerUrl_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_AppServerUrl_Description))]
        [LocalizedCategory(nameof(Resources.Server_Category))]
        public InArgument<string> AppServerUrl { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_AppServerDsn_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_AppServerDsn_Description))]
        [LocalizedCategory(nameof(Resources.Server_Category))]
        public InArgument<string> AppServerDsn { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_LicenseType_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_LicenseType_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public Hyland.Unity.LicenseType LicenseType { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_Username_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_Username_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        public InArgument<string> Username { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_Password_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_Password_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        public InArgument<string> Password { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_SessionID_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_SessionID_Description))]
        [LocalizedCategory(nameof(Resources.Session_Category))]
        public InOutArgument<string> SessionID { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_Connected_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_Connected_Description))]
        [LocalizedCategory(nameof(Resources.Session_Category))]
        public InOutArgument<bool> Connected { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseOnBaseUnity_Message_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseOnBaseUnity_Message_Description))]
        [LocalizedCategory(nameof(Resources.Session_Category))]
        public OutArgument<string> Message { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "ScopeActivity";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private readonly IObjectContainer _objectContainer;

        #endregion


        #region Constructors

        public UseOnBaseUnity(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;

            Body = new ActivityAction<IObjectContainer>
            {
                Argument = new DelegateInArgument<IObjectContainer> (ParentContainerPropertyTag),
                Handler = new Sequence { DisplayName = Resources.Do }
            };
        }

        public UseOnBaseUnity() : this(new ObjectContainer())
        {

        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (AppServerUrl == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(AppServerUrl)));
            if (SessionID == null && AppServerDsn == null) metadata.AddValidationError(" ServerDSN or SessionID must be provided. ");

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext  context, CancellationToken cancellationToken)
        {
            // Inputs
            var appserverurl = AppServerUrl.Get(context);
            var appserverdsn = AppServerDsn.Get(context);
            Hyland.Unity.LicenseType licensetype = LicenseType;
            var username = Username.Get(context);
            var password = Password.Get(context);
            var sessionid = SessionID.Get(context);
            var connected = Connected.Get(context);
            var continueonerror = ContinueOnError.Get(context);

            if (connected != null) { keepConnected = connected; }
            connected = false;


            try
            {
                msg = licensetype.ToString();
                if (keepConnected && sessionid.Length > 0)
                {
                    try
                    {
                        if (obApp != null) { msg = obApp.SessionID; }

                        SessionIDAuthenticationProperties authProps = Hyland.Unity.Application.CreateSessionIDAuthenticationProperties(appserverurl, sessionid, false);
                        obApp = Hyland.Unity.Application.Connect(authProps);
                    } catch (Exception ex)
                    {
                        throw new Exception("Unable to Reconnect to Session: " + sessionid + " " + ex.Message);
                    }
                }
                else
                {
                    if (!username.Replace(" ", "").ToLower().Contains("currentuser"))  // this will allow "<currentuser>||currentuser||Current User"||etc. to indicate to use NT Auth
                    {
                        Hyland.Unity.AuthenticationProperties 
                        authProps = Hyland.Unity.Application.CreateOnBaseAuthenticationProperties(appserverurl, username, password, appserverdsn);
                        authProps.IsDisconnectEnabled = false;
                        authProps.LicenseType = licensetype;
                        obApp = Hyland.Unity.Application.Connect(authProps);
                    }
                    else
                    {
                        Hyland.Unity.DomainAuthenticationProperties
                        authProps = Hyland.Unity.Application.CreateDomainAuthenticationProperties(appserverurl, appserverdsn);
                        authProps.IsDisconnectEnabled = false;
                        authProps.LicenseType = licensetype;
                        obApp = Hyland.Unity.Application.Connect(authProps);
                    }
                }

                sessionid = obApp.SessionID;
                connected = true;

                if (Body != null)
                {
                    _objectContainer.Add(appserverurl + "~" + sessionid);
                    return (ctx) =>
                    {
                        // Schedule child activities
                            ctx.ScheduleAction<IObjectContainer>(Body, _objectContainer, OnCompleted, OnFaulted);
                        connected = (obApp != null);
                        if (connected = false)
                        {
                            sessionid = "";
                            msg = "Disconnected";
                        }
                        // Outputs
                        AppServerUrl.Set(ctx, appserverurl);
                        SessionID.Set(ctx, sessionid);
                        Connected.Set(ctx, connected);
                        Message.Set(ctx, msg);
                    };
                } else 
                {
                    return (ctx) => {
                    AppServerUrl.Set(ctx, appserverurl);
                    SessionID.Set(ctx, sessionid);
                    Connected.Set(ctx, connected);
                    Message.Set(ctx, msg);
                    };
                }
            }
            catch (Exception ex)
            {
                msg = "Error Connecting:\t" + ex.ToString();
                throw new SystemException(msg);
            }
        }

        #endregion


        #region Events

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Cleanup();
            if (!keepConnected && obApp != null)
            {
                try 
                {
                    obApp.Disconnect();
                    obApp = null;
                }
                catch (Exception ex) { }
            } 
        }

        #endregion


        #region Helpers
        
        private void Cleanup()
        {
            var disposableObjects = _objectContainer.Where(o => o is IDisposable);
            foreach (var obj in disposableObjects)
            {
                if (obj is IDisposable dispObject)
                    dispObject.Dispose();
            }
            _objectContainer.Clear();
        }

        #endregion
    }
}

