using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Data.Items;
using Sitecore.Analytics.Pipelines.RegisterPageEvent;
using Sitecore.Commerce.Automation.MarketingAutomation;
using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Entities.Carts;
using Sitecore.Commerce.Entities.Orders;
using Sitecore.Commerce.Pipelines;
using Sitecore.Commerce.Services.Orders;
using Sitecore.Diagnostics;

namespace Sitecore.Support.Commerce.Pipelines.Orders
{
    public class TriggerOrderedProductStockStatusPageEvent : Sitecore.Commerce.Connect.CommerceServer.Orders.Pipelines.CommerceTriggerProductStockStatusPageEvent
    {
        public TriggerOrderedProductStockStatusPageEvent(IEntityFactory entityFactory) : base(entityFactory)
        {
        }

        public override void Process(ServicePipelineArgs args)
        {
            Assert.IsTrue(args.Request is SubmitVisitorOrderRequest, "args.Request must be of type SubmitVisitorOrderRequest");
            Assert.IsTrue(args.Result is SubmitVisitorOrderResult, "args.Result must be of type SubmitVisitorOrderResult");
            SubmitVisitorOrderResult submitVisitorOrderResult = (SubmitVisitorOrderResult)args.Result;
            if (submitVisitorOrderResult.Order != null)
            {
                string shopName = submitVisitorOrderResult.Order.ShopName;
                Order order = submitVisitorOrderResult.Order;
                IEnumerable<CartProduct> lineItemsForPageEvents = GetLineItemsForPageEvents(shopName, order);
                foreach (Dictionary<string, object> pageEventDatum in GetPageEventData(shopName, lineItemsForPageEvents))
                {
                    RaisePageEvent(pageEventDatum, args);
                }
                args.Result.Success = true;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaisePageEvent(Dictionary<string, object> data, ServicePipelineArgs args)
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            if (Tracker.Current == null)
            {
                if (CommerceAutomationHelper.PageEventsEnabled)
                {
                    Log.Error(string.Format(CultureInfo.InvariantCulture, "The page event {0} cannot be registered because Tracker.Current is not initialized", new object[1]
                    {
                        Name
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

            PageEventItem pageEventItem = null;
            pageEventItem = (Tracker.DefinitionItems.Goals[Name] ?? Tracker.DefinitionItems.PageEvents[Name]);
            if (pageEventItem == null)
            {
                Log.Error(string.Format(CultureInfo.InvariantCulture, "Page event definition {0} not found, page URL {1}", new object[2]
                {
                Name,
                Tracker.Current.CurrentPage.Url
                }), this);
                args.Result.Success = false;
                return;
            }
            Sitecore.Analytics.Model.PageEventData pageEventData = Tracker.Current.CurrentPage.Register(pageEventItem);
            pageEventData.Text = GetPageEventText(args);
            foreach (KeyValuePair<string, object> datum in data)
            {
                pageEventData.CustomValues[datum.Key] = datum.Value;
            }
            RegisterPageEventPipeline.Run(Tracker.Current.CurrentPage.Session, pageEventData, pageEventItem);
        }
    }
}