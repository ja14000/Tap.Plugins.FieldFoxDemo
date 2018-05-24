//<copyright>
// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Keysight.Tap;

// <template info>
//Note this template assumes that you have a SCPI based instrument, and accordingly
//extends the ScpiInstrument base class.

//If you do NOT have a SCPI based instrument, you should modify this instance to extend
//the (less powerful) Instrument base class.
//</template info>

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FieldFox", Group: "FieldFoxDemoInstruments", Description: "FieldFox N9917A")]
    [ShortName("FieldFox")]
    public class FieldFox : ScpiInstrument
    {
        #region Settings
        // ToDo: Add property here for each parameter the end user should be able to change
        #endregion
        public FieldFox()
        {
            //ScpiCommand("SYSTem:PRESet");
        }

        public override void Open()
        {
            base.Open();
            if (!IdnString.Contains("N9"))
            {
                Log.Error("This instrument driver does not support the connected instrument.");
                throw new ArgumentException("Wrong instrument type.");
            }
        }

        public override void Close()
        {
            // TODO:  Shut down the connection to the instrument here.
            base.Close();
        }

        //<summary>
        //Select the Spectrum Analyzer Instrument
        //Activate FM Wide listening mode
        //Tune to radio station
        //Turn On the Pre-Amp
        //</summary>
        public void RadioMode(double StationFrequency)
        {
            ScpiCommand(@"INSTrument:SELect ""SA"""); 
            ScpiCommand("SENSe:MEASurement:TAListen FMW"); 
            ScpiCommand(":SENSe:TAListen:TFReq " + StationFrequency); 
            ScpiCommand(":SENSe:POWer:RF:GAIN:STATe 1"); 
        }

        //<summary>
        //Set the center frequency to the value held in the CenterFrequecy
        //<summary>
        public void SAView(double CenterFrequency)
        {
            ScpiCommand(":SENSe:FREQuency:CENTer " + CenterFrequency); 
        }

        //<summary>
        //Set Start Frequency
        //Set StopFrequency
        //<summary>
        public void ScanStations(double StartFrequency, double StopFrequency)
        {
            ScpiCommand(":SENS:FREQ:STAR " + StartFrequency); 
            ScpiCommand(":SENS:FREQ:STOP " + StopFrequency); 
        }

        public double[] GetData()
        {
            ScpiCommand("SWE:POIN 401"); 
            ScpiCommand("TRAC1:TYPE AVG"); 
            ScpiCommand("DISPlay:WINDow:TRACe:Y:SCALe:AUTO");
            return ScpiQuery<double[]>("TRAC1:DATA?");
        }
        
        //<summary>
        // This function generates a list of frequencies based on the start frequency, stop frequency and number of points that are
        // displayed on the FieldFox. These frequency values are used on the x-axis of the 'FM Spectrum View' plot and for later operations.
        //</summary>
        public List<double> CalcFrequency(double StartFrequency, double StopFrequency)
        {
            var FrequencyStep = ((StopFrequency - StartFrequency) / (401));
            List<double> FrequencyList = new List<double>();
            FrequencyList.Add(StartFrequency);

            for (int runs = 0; runs < 400; runs++)
            {
                FrequencyList.Add(FrequencyList[runs] + FrequencyStep);
            }
            return FrequencyList; 
        }

        //<summary>
        // Returns a list of Amplitudes from the MeasurementResults array that have a value greater than the 'AmplitudeCutoff' variable
        //</summary>
        public double[] AmplitudesAboveCutoff(int AmplitudeCutOff, double[] MeasurementResults)
        {
            var AmplitudesAboveCutoff = MeasurementResults.Where(item => item >= AmplitudeCutOff).ToArray();
            return AmplitudesAboveCutoff; 
        }

        //<summary>
        // This function takes the array of amplitude data gathered by the fieldfox(MeasurementResults) and for any
        // amplitude value greater than the 'AmplitudeCutOff' variable it adds the 'FrequencyList' value with the same
        // index array to a new array called 'FrequenciesAboveCutoff'array is returned in the last step. We now have
        // an array of Frequency values for the AmplitudesAboveCutoff array created in the previous step.
        //</summary>
        public List<double> FrequenciesAboveCutoff(int AmplitudeCutOff, double[] MeasurementResults, List<double> FrequencyList)
        {
            List<double> FrequenciesAboveCutoff = new List<double>();
            var x = 0;
            foreach (double i in MeasurementResults)
            {
                if (MeasurementResults[x] > AmplitudeCutOff)
                {
                    FrequenciesAboveCutoff.Add(FrequencyList[x]);
                    x++;
                }
                else
                {
                    x++;
                }
            }
            return FrequenciesAboveCutoff; 
        }

        //<summary>
        // Creates an array of evenly spaced channels based on the start and stop frequency, these are
        // used later as a the reference for the AmplitudesAboveCutoff to be sorted in to.
        //</summary>
        public List<double> CreateChannels()
        {
            var step = ((108000000 - 88000000) / (100));
            List<double> ChannelsCreated = new List<double>();
            ChannelsCreated.Add(88000000);
            var x = 0;
            double stop = 88000000;
            while (stop < 108000000)
            {
                ChannelsCreated.Add(ChannelsCreated[x] + step);
                x++;
                stop += step;
            }
            return ChannelsCreated;
        }

        //<summary>
        // This Function sorts the 'FrequenciesAboveCutoff' values to into their respective channels 
        //</summary>
        public List<double> MapFrequencyToChannel(List<double> FrequenciesAboveCutoff, List<double> ChannelsCreated)
        {
            var x = 0;
            var y = 1;
            List<double> ChannelMappedFrequencies = new List<double>();
            for (int i = 0; i < 100; i++)
            {
                var MatchFound = false;
                if (FrequenciesAboveCutoff[x] >= ChannelsCreated[x] && FrequenciesAboveCutoff[x] <= ChannelsCreated[y])
                {
                    MatchFound = true;
                }
                if (MatchFound == true)
                {
                    ChannelMappedFrequencies.Add(ChannelsCreated[x]);
                    y++;
                    x++;
                }
                if (MatchFound == false)
                {
                    y++;
                }
                else if (x == FrequenciesAboveCutoff.Count - 1) //remove the -1 on next run
                {
                    break;
                }
            }
            return ChannelMappedFrequencies;
        }

        //<summary>
         //This function returns a list of amplitudes for the list of channels, it uses the index of the Frequency value to find the
         //correspoding amplitude value and adds this value to an array that is returned at the end. This function in combination with the
        // 'FrequenciesForChannels' function also ensures that the final list of Amplitude values is the same size as the final list of
        // frequency values so that they can be plotted
        //</summary>

        public List<double> AmplitudesForChannels(List<double> AmplitudesAboveCutoff, List<double> FrequenciesMappedToChannels)
        {
            List<double> FinalAmplitudeList = new List<double>();
            var length = AmplitudesAboveCutoff.Count;
            var x = 0;

            foreach (double i in FrequenciesMappedToChannels)
                while (x <= length - 1)
                {
                    {
                        FinalAmplitudeList.Add(AmplitudesAboveCutoff[FrequenciesMappedToChannels.IndexOf(i)]);
                        x++;
                    }
                }

            return FinalAmplitudeList;
        }

        //<summary>
        // Returns a list of frequencies the same size as the list of amplitudes.
        //</summary>
        public List<double> FrequenciesForChannels(List<double> AmplitudesAboveCutoff, List<double> FrequenciesMappedToChannels)
        {
            List<double> FinalFrequencyList = new List<double>();
            var length = FrequenciesMappedToChannels.Count;
            var x = 0;

            foreach (double i in AmplitudesAboveCutoff)
                while (x <= length - 1)
                {
                    {
                        FinalFrequencyList.Add(FrequenciesMappedToChannels[AmplitudesAboveCutoff.IndexOf(i)]);
                        x++;
                    }
                }
            return FinalFrequencyList;

        }

        //public List<double> MapFrequencyToChannel(List<double> FrequenciesAboveCutoff, List<double> ChannelsCreated)
        //{
        //    var x = 0;
        //    var y = 1;
        //    var MatchFound = false;

        //    List<double> ChannelMappedFrequencies = new List<double>();
        //    for (int i = 0; i < 100; i++)
        //    {

        //        while (MatchFound == false)
        //        {
        //            if (FrequenciesAboveCutoff[x] >= ChannelsCreated[x] && FrequenciesAboveCutoff[x] <= ChannelsCreated[y])
        //            {
        //                ChannelMappedFrequencies.Add(ChannelsCreated[x]);
        //                MatchFound = true;
        //            }
        //            else
        //            {
        //                y++;

        //            }
        //        }

        //        x++;
        //        MatchFound = false;
        //    }
        //    return ChannelMappedFrequencies;
        //}


        //public List<double> MapToChannel(List<double> FreqienciesFoundList, List<double> ChannelsFoundList)

        //{
        //    var x = 0;
        //    var u = 0;
        //    var y = 1;
        //    var z = FreqienciesFoundList.Count;
        //    List<double> FinalStationList = new List<double>();
        //    foreach (double i in ChannelsFoundList)

        //        if (FreqienciesFoundList[x] >= ChannelsFoundList[x] && FreqienciesFoundList[x] <= ChannelsFoundList[y])
        //        {


        //            if (x < (FreqienciesFoundList.Count - 1) )
        //            {
        //                //x = FreqienciesFoundList.Count;
        //                FinalStationList.Add(ChannelsFoundList[x]);
        //                x++;
        //                u++;
        //            }

        //            else if ( x >= (FreqienciesFoundList.Count - 1))
        //            {
        //                FinalStationList.Add(ChannelsFoundList[u+y-1]);
        //                u++;
        //            }
        //        }
        //        else
        //        {
        //            y++;
        //        }
        //    return FinalStationList;
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FreqienciesFoundList"></param>
        /// <param name="ChannelsFoundList"></param>
        /// <returns></returns>
        /// 


        //public List<double> CreateStations(double[] MeasurementResults, List<double> FrequenciesFound, double StartFrequency, double StopFrequency)
        //{
        //    List<double> MeasurementResultsList = MeasurementResults.ToList<double>();
        //    List<double> StationChannelList = new List<double>();
        //    List<double> StationsFound = new List<double>();
        //    StationsFound.Add(87.5);
        //    int x = 0;
        //    var ChannelWidth = 2000000;

        //    var NoOfChannels = (StopFrequency - StartFrequency) / (ChannelWidth);

        //    foreach (double i in FrequenciesFound)
        //    {
        //        StationChannelList.Add(StationsFound[x] + ChannelWidth);
        //        x++;
        //    }

        //    return x;

    }

}


