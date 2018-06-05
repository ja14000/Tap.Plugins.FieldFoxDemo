// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Keysight.Tap; 

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FF Demo", Group: "FieldFoxDemo", Description: "Tune in to a radio station, capture data, plot graphs")]

    public class Step : TestStep
    {
        #region Settings

        //Creates a UI dropdown for the variable
        [Display("Station Frequency", Group: "DUT Setup", Order: 1)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StationFrequency { get; set; }

        [Display("Center Frequency", Group: "DUT Setup", Order: 2)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double CenterFrequency { get; set; }

        [Display("Start Frequency", Group: "DUT Setup", Order: 3)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StartFrequency { get; set; }

        [Display("Stop Frequency", Group: "DUT Setup", Order: 4)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StopFrequency { get; set; }

        [Display("Amplitude Cut Off", Group: "DUT Setup", Order: 5)]
        [Unit("dBm", UseEngineeringPrefix: true)]
        public int AmplitudeCutOff { get; set; }

        [Display("Freeze Fieldfox Display", Group: "Other Settings", Order: 3)]
        public bool FreezeFF { get; set; }

        [Display("Include GPS Data", Group: "Other Settings", Order: 2)]
        public bool IncludeGPS { get; set; }

        [Display("Play Radio Station on Speakers", Group: "Other Settings", Order: 4)]
        public bool PlayYesNo { get; set; }

        [Display("Preset Instrument", Group: "Other Settings", Order: 1)]
        public bool PresetYesNo { get; set; }

        [Display("Enable Step Verdict", Group: "Other Settings", Order: 5)]
        public bool EnableTestVerdict { get; set; }
        public Verdict MyVerdict;


        // Instrument Declarations (Creates dropdown in TAP GUI))
        [Display("FieldFox", Group: "DUT", Order: 1)]
        public FieldFox FF { get; set; }


       
        #endregion

        public Step()
        {
            //Set default values for properties / settings.
            StationFrequency = 104.1e6;
            CenterFrequency = StationFrequency;
            StartFrequency = 88000000;
            StopFrequency = 108000000;
            AmplitudeCutOff = -80;
            PresetYesNo = false;
            IncludeGPS = true;
            FreezeFF = true;
            PlayYesNo = false;
            EnableTestVerdict = true;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public void SetVerdict()
        {
            MyVerdict = Verdict.NotSet;
        }

        public override void Run()
        {
            FF.Preset(PresetYesNo);
            FF.RadioMode(StationFrequency, PlayYesNo);
            FF.SAView(CenterFrequency);
            FF.ScanStations(StartFrequency, StopFrequency);

            //Initial array of amplitudes collected by the fieldfox
            var MeasurementResults = FF.GetData(FreezeFF);

            //Round MeasurementResults from FieldFox
            var RoundedMeasurementResultsList = FF.RoundMeasurements(MeasurementResults);
            var RoundedMeasurementResultsArray = RoundedMeasurementResultsList.ToArray();

            // Initial array of frequencies evenly spaced between start and stop value
            var FrequencyList = FF.CalcFrequency(StartFrequency, StopFrequency);
            var FrequencyArray = FrequencyList.ToArray();

            //Array of Amplitudes greater than the amplitue cutoff i.e. 'Stations'
            var AmplitudesAboveCutoffArray = FF.AmplitudesAboveCutoff(AmplitudeCutOff, MeasurementResults);
            var StationsFoundList = AmplitudesAboveCutoffArray.ToList();

            // List of frquencies for each 'Station(Amplitude)' value
            var FrequenciesFoundList = FF.FrequenciesAboveCutoff(AmplitudeCutOff, MeasurementResults, FrequencyList);
            var FrequenciesFoundArray = FrequenciesFoundList.ToArray();

            //String containing GPS Data
            string GPSDATA = FF.GetGPS();
            // string[] GPSARRAY = new string[] { GPSDATA }; -- This is used for plotting the gps data as a data table rather than using them in the table heading.

            
            if (IncludeGPS == true)
            {
                Results.PublishTable("Location/Date/Time of Scan: " + GPSDATA, new List<string> { "Frequency(Hz)", "Amplitude(dBm)" }, FrequencyArray, RoundedMeasurementResultsArray);
            }
            else
            {
                Results.PublishTable("Channels Frequencies", new List<string> { "Frequency(Hz)", "Amplitude(dBm)" }, FrequencyArray, RoundedMeasurementResultsArray);
            }

            Results.PublishTable("Frequencies Above Cutoff", new List<string> { "Station Frequency(Hz)", "Station Amplitude(dBm)" }, FrequenciesFoundArray, AmplitudesAboveCutoffArray);


            if (EnableTestVerdict == true)
            {
                if (FrequencyArray[1] > 0 && RoundedMeasurementResultsArray[1] < 0 && FrequenciesFoundArray[1] > 0 && AmplitudesAboveCutoffArray[1] < 0)
                {
                    UpgradeVerdict(Verdict.Pass);
                }

                else
                {
                    UpgradeVerdict(Verdict.Fail);
                }

            }

            //if (IncludeGPS == true)
            //{
            //    Results.PublishTable("Scan @ Coordinates: ", new List<string> { "GPS Coordinates", }, GPSARRAY);
            //}
        }


        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }

 
}