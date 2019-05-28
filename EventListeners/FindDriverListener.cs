using System;
using System.Collections.Generic;
using System.Linq;
using Android.Gms.Maps.Model;
using Com.Google.Maps.Android;
using Firebase.Database;
using Uber_Rider.DataModels;
using Uber_Rider.Helpers;

namespace Uber_Rider.EventListeners
{
    public class FindDriverListener : Java.Lang.Object, IValueEventListener
    {

        //Events
        public class DriverFoundEventArgs : EventArgs
        {
            public List<AvailableDriver> Drivers { get; set; }
        }

        public event EventHandler<DriverFoundEventArgs> DriversFound;
        public event EventHandler DriverNotFound;

        //Ride Details
        LatLng mPickupLocation;
        string mRideId;

        //Available Drivers
        List<AvailableDriver> availableDrivers = new List<AvailableDriver>();

        public FindDriverListener(LatLng pickupLocation, string rideid)
        {
            mPickupLocation = pickupLocation;
            mRideId = rideid;
        }

        public void OnCancelled(DatabaseError error)
        {

        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                var child = snapshot.Children.ToEnumerable<DataSnapshot>();
                availableDrivers.Clear();

                foreach(DataSnapshot data in child)
                {
                    if(data.Child("ride_id").Value != null)
                    {
                        if(data.Child("ride_id").Value.ToString() == "waiting")
                        {
                            //Get Driver Location;
                            double latitude = double.Parse(data.Child("location").Child("latitude").Value.ToString());
                            double longitude = double.Parse(data.Child("location").Child("longitude").Value.ToString());
                            LatLng driverLocation = new LatLng(latitude, longitude);
                            AvailableDriver driver = new AvailableDriver();

                            //Compute Distance Between Pickup Location and Driver Location
                            driver.DistanceFromPickup = SphericalUtil.ComputeDistanceBetween(mPickupLocation, driverLocation);
                            driver.ID = data.Key;
                            availableDrivers.Add(driver);

                        }
                    }
                }

                if(availableDrivers.Count > 0)
                {
                    availableDrivers = availableDrivers.OrderBy(o => o.DistanceFromPickup).ToList();
                    DriversFound?.Invoke(this, new DriverFoundEventArgs { Drivers = availableDrivers });
                }
                else
                {
                    DriverNotFound.Invoke(this, new EventArgs());
                }
            }
            else
            {
                DriverNotFound.Invoke(this, new EventArgs());
            }
        }

        public void Create()
        {
            FirebaseDatabase database = AppDataHelper.GetDatabase();
            DatabaseReference findDriverRef = database.GetReference("driversAvailable");
            findDriverRef.AddListenerForSingleValueEvent(this);
        }
    }
}
