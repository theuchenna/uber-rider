using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Uber_Rider.EventListeners
{
    public class TaskCompletionListener : Java.Lang.Object, IOnSuccessListener, IOnFailureListener
    {
        public event EventHandler Success;
        public event EventHandler Failure;
        public void OnFailure(Java.Lang.Exception e)
        {
            Failure?.Invoke(this, new EventArgs());
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            Success?.Invoke(this, new EventArgs());
        }
    }
}