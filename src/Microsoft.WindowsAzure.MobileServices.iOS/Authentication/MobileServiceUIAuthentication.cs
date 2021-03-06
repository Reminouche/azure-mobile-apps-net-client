﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Xamarin.Auth._MobileServices;
#if __UNIFIED__
using Foundation;
using UIKit;
using NSAction = System.Action;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Microsoft.WindowsAzure.MobileServices
{
    internal class MobileServiceUIAuthentication : MobileServiceAuthentication
    {
        private readonly RectangleF rect;
        private readonly object view;

        public MobileServiceUIAuthentication(RectangleF rect, object view, IMobileServiceClient client, string providerName, IDictionary<string, string> parameters)
            : base(client, providerName, parameters)
        {
            this.rect = rect;
            this.view = view;
        }

        protected override Task<string> LoginAsyncOverride(string title = "")
        {
            var tcs = new TaskCompletionSource<string>();

            var auth = new WebRedirectAuthenticator(StartUri, EndUri);

            auth.ShowUIErrors = false;
            auth.ClearCookiesBeforeLogin = false;
            if (!string.IsNullOrEmpty(title))
            {
                auth.Title = title;
            }
            
            UIViewController c = auth.GetUI();

            UIViewController controller = null;
            UIPopoverController popover = null;

            auth.Error += (o, e) =>
            {
                NSAction completed = () =>
                {
                    Exception ex = e.Exception ?? new Exception(e.Message);
                    tcs.TrySetException(ex);
                };

                if (controller != null)
                    controller.DismissViewController(true, completed);
                if (popover != null)
                {
                    popover.Dismiss(true);
                    completed();
                }
            };

            auth.Completed += (o, e) =>
            {
                NSAction completed = () =>
                {
                    if (!e.IsAuthenticated)
                        tcs.TrySetException(new InvalidOperationException("Authentication was cancelled by the user."));
                    else
                        tcs.TrySetResult(e.Account.Properties["token"]);
                };

                if (controller != null)
                    controller.DismissViewController(true, completed);
                if (popover != null)
                {
                    popover.Dismiss(true);
                    completed();
                }
            };

            controller = view as UIViewController;
            if (controller != null)
            {
                controller.PresentViewController(c, true, null);
            }
            else
            {
                UIView v = view as UIView;
                UIBarButtonItem barButton = view as UIBarButtonItem;

                popover = new UIPopoverController(c);

                if (barButton != null)
                    popover.PresentFromBarButtonItem(barButton, UIPopoverArrowDirection.Any, true);
                else
                    popover.PresentFromRect(rect, v, UIPopoverArrowDirection.Any, true);
            }

            return tcs.Task;
        }
    }
}
