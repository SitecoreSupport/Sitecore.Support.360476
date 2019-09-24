using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Sitecore.Analytics;
using Sitecore.Commerce.Automation.MarketingAutomation;
using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Multishop;
using Sitecore.Commerce.Pipelines;
using Sitecore.Commerce.Services.Orders;
using Sitecore.Diagnostics;

namespace Sitecore.Support.Commerce.Pipelines.Orders
{
    public class AddOrderToEAPlan : Sitecore.Commerce.Pipelines.Orders.Common.AddOrderToEAPlan
    {
        public AddOrderToEAPlan(IEaPlanProvider eaPlanProvider, IEntityFactory entityFactory) : base(eaPlanProvider, entityFactory)
        {
        }

        public override void Process(ServicePipelineArgs args)
        {
            try
            {
                base.Process(args);
            }
            catch (NullReferenceException)
            {
                if (Tracker.Current == null)
                {
                    if (CommerceAutomationHelper.PageEventsEnabled)
                    {
                        Log.Error(string.Format(CultureInfo.InvariantCulture, "The page event {0} cannot be registered because Tracker.Current is not initialized", new object[1]
                        {
                            this.GetType().Name
                        }), this);

                        args.Result.Success = false;
                    }
                    return;
                }
                else if (Tracker.Current.CurrentPage == null)
                {
                    string currentUri = HttpContext.Current?.Request?.Url?.ToString() ?? String.Empty;
                    Log.Error($"The page '{currentUri}' can't be tracked, Tracker.Current.CurrentPage is not initialized in processor '{this.GetType()}'", this);
                }
            }
        }
    }
}