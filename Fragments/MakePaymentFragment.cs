
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Uber_Rider.Fragments
{
    public class MakePaymentFragment : Android.Support.V4.App.DialogFragment
    {
        double mfares;
        TextView totalFaresText;
        Button makePaymentButton;

        public event EventHandler PaymentCompleted;

        public MakePaymentFragment(double fares)
        {
            mfares = fares;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
         View view =  inflater.Inflate(Resource.Layout.makepayment, container, false);
            totalFaresText = (TextView)view.FindViewById(Resource.Id.totalfaresText);
            makePaymentButton = (Button)view.FindViewById(Resource.Id.makePaymentButton);

            totalFaresText.Text = "$" + mfares.ToString();
            makePaymentButton.Click += MakePaymentButton_Click;

            return view;
        }

        void MakePaymentButton_Click(object sender, EventArgs e)
        {
            PaymentCompleted.Invoke(this, new EventArgs());
        }

    }
}
