using System;
using ININ.InteractionClient.AddIn;
using ININ.IceLib.People;
using System.ComponentModel;
using ININ.IceLib.Connection;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Busylight;

namespace OHBusyLight
{
    public class InteractionDesktopBusyLight : IAddIn
    {
        internal Busylight.SDK busylight = null;

        private UserStatusList _SessionUserStatusList;

        private PeopleManager _people;
        private Session _session = null;
        private ColorConfig _colorConfig;
        INotificationService _notification = null;
        private System.Timers.Timer _timer = new System.Timers.Timer();
        public string _currentcolor;
        private int _rate = 0;


        public void Load(IServiceProvider serviceProvider)
        {
            _notification = (INotificationService)serviceProvider.GetService(typeof(INotificationService));
            _timer.Stop();
            _timer.Elapsed +=TimerElasped;
            

            busylight = new Busylight.SDK(true);

            //Check to see if they have one attached, else why load?
            var list = busylight.GetAttachedBusylightDeviceList();
            if (list.Length == 0)
            {
                //_notification.Notify("No Busy Light Detected. Stopping Status Light App Load", "Not Detected", NotificationType.Info,TimeSpan.FromSeconds(10));
                return;
            }

            //Lets look for a config file
            ConfigLoad();

            //got this far try to get a session and then a People manager instance so that we can watch a user's status
            try
            {
                _session = (Session)serviceProvider.GetService(typeof(Session));

                if (_session != null)
                {
                    try
                    {
                        _people = PeopleManager.GetInstance(_session);

                        if (_people != null)
                        {
                            StartWatch();
                        }

                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }





        }
        public void ConfigLoad()
        {

            string localLocation = System.Environment.ExpandEnvironmentVariables("%userprofile%/downloads/") + "ColorProfile.json";

            //lets see if one already exists and if they do load it into memory
            if (File.Exists(@localLocation))
            {
                using (StreamReader file = File.OpenText(@localLocation))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _colorConfig = (ColorConfig)serializer.Deserialize(file, typeof(ColorConfig));

                }

            }
            else //go get the default and store it locally
            {
                string fileLocation = @"I:\apps\ININColorSchemes\Default.json";

                if (File.Exists(@fileLocation))
                {
                    using (StreamReader file = File.OpenText(fileLocation))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        _colorConfig = (ColorConfig)serializer.Deserialize(file, typeof(ColorConfig));


                    }
                }

            }
        }


        public void Unload()
        {
            //Clean up our stuff
            busylight = null;
            StopWatch();

            if (_session != null)
            {
                if (_session.ConnectionState == ConnectionState.Up)
                {
                    _session.Disconnect();
                }
                _session.Dispose();
                _session = null;


            }


        }

        public void StartWatch()
        {
            try
            {
                _SessionUserStatusList = new UserStatusList(_people);


                _SessionUserStatusList.WatchedObjectsChanged += SessionUserStatusChanged;

                // Start a watch on the current session user async
                string[] userToWatch = new string[1];
                userToWatch[0] = _people.Session.UserId;


                _SessionUserStatusList.StartWatchingAsync(userToWatch, UserStatusListStartWatchingCompleted, null);


                /*Used this for testing - If it got here then everything was good
                  
                _notification.Notify("Starting User Status Watch", "Start Watch", NotificationType.Info, TimeSpan.FromSeconds(10));

                */
            }
            catch (Exception)
            {

            }
        }
        public void StopWatch()
        {
            try
            {
                /* Used this for testing to make sure it would kill it on the way out.
                
                _notification.Notify("Stoping User Status Watch", "Stop Watch", NotificationType.Info, TimeSpan.FromSeconds(10));
                
                */
                _SessionUserStatusList.StopWatchingAsync(null, null);
                _SessionUserStatusList.WatchedObjectsChanged -= SessionUserStatusChanged;

                busylight = null;

            }
            catch (Exception)
            {

                throw;
            }

        }

        private void SessionUserStatusChanged(Object sender, WatchedObjectsEventArgs<UserStatusProperty> e)
        {
            try
            {
                UpdateUsersStatus(e);
            }
            catch (Exception)
            {

            }
        }

        private void UserStatusListStartWatchingCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    UserStatus status = _SessionUserStatusList[_people.Session.UserId];

                    if (status == null)
                        return;

                    if (status.StatusMessageDetails != null)
                    {

                        BusyLightUpdate(status);

                    }


                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
        }
        private void UpdateUsersStatus(WatchedObjectsEventArgs<UserStatusProperty> objectChanged)
        {

            try
            {
                if (_SessionUserStatusList != null)
                {

                    if (objectChanged != null)
                    {
                        string userId = _people.Session.UserId;

                        UserStatus status = _SessionUserStatusList[userId];

                        if (status != null)
                        {

                            BusyLightUpdate(status);

                        }
                    }

                }
            }
            catch (Exception)
            {

            }
        }

        private void BusyLightUpdate(UserStatus status)
        {
            //create it just in cast it isn't already, should be though at this point
            if (busylight == null)
            {
                busylight = new Busylight.SDK(true);
            }

            _timer.Stop();

            //try to get the color by status message or type from the file that is loaded / populated at load 
            var type = _colorConfig.StatusTypes.Where(x => x.Name.ToUpper() == status.StatusMessageDetails.GroupTag.ToUpper()).Select(x => x).FirstOrDefault();
            var message = _colorConfig.StatusMessages.Where(x => x.Name.ToUpper() == status.StatusMessageDetails.MessageText.ToUpper()).Select(x => x).FirstOrDefault();

            //var messageColor = _colorConfig.StatusMessages.Where(x => x.Name.ToUpper() == status.StatusMessageDetails.MessageText.ToUpper()).Select(x => x.Color).FirstOrDefault();
            //var IDmessage = status.StatusMessageDetails.MessageText;
            //var statusColor = _colorConfig.StatusTypes.Where(x => x.Name.ToUpper() == status.StatusMessageDetails.GroupTag.ToUpper()).Select(x => x.Color).FirstOrDefault();
            //var IDgroup = status.StatusMessageDetails.GroupTag;
            //_notification.Notify(message.Name + " " + message.Type + " " + message.BlinkDelay.ToString() + " " + message.Color + " " + IDgroup, "message", NotificationType.Info,TimeSpan.FromSeconds(10));
            //_notification.Notify(type.Name + " " + type.BlinkDelay.ToString() + " " + type.Color + " " + IDgroup , "group", NotificationType.Info,TimeSpan.FromSeconds(10));



            //if there is a individual message color, use that
            if (!string.IsNullOrEmpty(message.Name))
            {
                ColorCase(message.Color);

                //lets start a timer if the message has a value
                if (message.BlinkDelay > 0)
                {
                    _timer.Interval = message.BlinkDelay * 1000;
                    _timer.Start();
                    _rate = message.BlinkRate;
                }
                else if (type.BlinkDelay > 0) //or if the default one does
                {
                    _timer.Interval = type.BlinkDelay * 1000;
                    _timer.Start();
                    _rate = type.BlinkRate;
                }

            }
            else //or use the type color
            {
                
                ColorCase(type.Color);
                 
                if (type.BlinkDelay > 0)
                {
                    _timer.Interval = type.BlinkDelay * 1000;
                    _timer.Start();
                    _rate = type.BlinkRate;
                }
            }


        }

        private void ColorCase(string message)
        {

            //BusylightColor color = new BusylightColor();
            
            switch (message)
            {
                case "GREEN":
                    busylight.Light(0,0,255);
                    _currentcolor = "GREEN";
                    //_notification.Notify("Green Color Case", "color case", NotificationType.Info, TimeSpan.FromSeconds(10));
                    break;

                case "YELLOW":
                    busylight.Light(255,0,255);
                    _currentcolor = "YELLOW";
                    break;

                case "BLUE":
                    busylight.Light(0,255,0);
                    _currentcolor = "BLUE";
                    break;

                case "RED":
                    busylight.Light(255,0,0);
                    _currentcolor = "RED";
                    break;

                case "PURPLE":
                    busylight.Light(128, 128, 0);
                    _currentcolor = "PURPLE";
                    break;

                case "ORANGE":
                    busylight.Light(255, 0, 40);
                    _currentcolor = "ORANGE";
                    break;

                case "WHITE":
                    busylight.Light(255, 255, 255);
                    _currentcolor = "WHITE";
                    break;

                default:
                    busylight.Light(0,0,255);
                    _currentcolor = "GREEN";
                    break;
            }
        }

        private void TimerElasped(Object source, System.Timers.ElapsedEventArgs e)
        {
            //Will make it start to blink if it has a blink rate
            if (_rate > 0)
            {
                switch (_currentcolor)
                {
                case "GREEN":
                    busylight.Blink(0, 0, 255, _rate, _rate);
                    break;
                case "YELLOW":
                    busylight.Blink(255, 0, 255, _rate, _rate);
                    break;
                case "BLUE":
                    busylight.Blink(0, 255, 0, _rate, _rate);
                    break;
                case "RED":
                    busylight.Blink(255, 0, 0, _rate, _rate);
                    break;
                case "PURPLE":
                    busylight.Blink(128, 128, 0, _rate, _rate);
                    break;
                case "WHITE":
                    busylight.Blink(255, 255, 255, _rate, _rate);
                    break;
                case "ORANGE":
                    busylight.Blink(255, 0, 40, _rate, _rate);
                    break;
                 default:
                    break;
                }

            }



            _timer.Stop();
        }

    }


}
