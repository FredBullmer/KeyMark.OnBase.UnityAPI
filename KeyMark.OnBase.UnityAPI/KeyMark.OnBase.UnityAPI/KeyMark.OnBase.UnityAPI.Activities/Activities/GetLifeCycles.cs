using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using KeyMark.OnBase.UnityAPI.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using Hyland.Unity.WorkView;
using Hyland.Unity;
using System.Windows.Interop;

namespace KeyMark.OnBase.UnityAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetLifeCycles_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetLifeCycles_Description))]
    public class GetLifeCycles : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetLifeCycles_LifeCycles_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetLifeCycles_LifeCycles_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<DataTable> LifeCycles { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "ScopeActivity";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private IObjectContainer _objectContainer;
        #endregion


        #region Constructors

        public GetLifeCycles()
        {
            base.Constraints.Add(ActivityConstraints.HasParentType<GetLifeCycles, UseOnBaseUnity>("Must reside inside UseOnBaseUnity Scope"));
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var continueonerror = ContinueOnError.Get(context);

            var properties = context.DataContext.GetProperties()[UseOnBaseUnity.ParentContainerPropertyTag];
            _objectContainer = properties.GetValue(context.DataContext) as IObjectContainer;
            string appserverurl = _objectContainer.Get<string>().Split("~")[0];
            string sessionid = _objectContainer.Get<string>().Split("~")[1];

            DataTable dtLCs = new DataTable();
            dtLCs.Columns.Add("LifeCycle");
            dtLCs.Columns.Add("ContentType");
            dtLCs.Columns.Add("QueueCount");

            try
            {
                SessionIDAuthenticationProperties authProps = Hyland.Unity.Application.CreateSessionIDAuthenticationProperties(appserverurl, sessionid, false);
                using (Hyland.Unity.Application obApp = Hyland.Unity.Application.Connect(authProps))
                {

                    // "http://kmobtest02.keymark.dom/appserver/service.asmx"

                    foreach (Hyland.Unity.Workflow.LifeCycle lc in obApp.Workflow.LifeCycles)
                    {
                        dtLCs.Rows.Add(lc.Name, lc.ContentType.ToString(), lc.Queues.Count.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (!continueonerror)
                {
                    throw new Exception(String.Format("Error getting list of Life Cycles from OnBase ({0})", ex.Message));
                }
            }
            // Outputs
            return (ctx) => {
                LifeCycles.Set(ctx, dtLCs);
            };
        }

        #endregion
    }
}

