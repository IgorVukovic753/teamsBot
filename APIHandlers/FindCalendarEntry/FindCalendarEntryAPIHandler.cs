using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamsAuth.APIHandlers;
using TeamsAuth.Config;

namespace TeamsAuth.APIHandlers.FindCalendarEntry
{
    public class FindCalendarEntryAPIHandler : APIHandler
    {

        public FindCalendarEntryAPIHandler(string authtoken) : base(authtoken)
        {

        }
        public async override Task<APIResult> ExecuteAPI(Intent obj)
        {
            APIResult result = new APIResult();
            result.IntentName = obj.IntentName;
            string response = "";

            try
            {
           
                string query = AssembleUrlQuery(obj);
               // response = await Get("https://graph.microsoft.com/v1.0/me/calendarview?startdatetime=2020-01-23T12:30:13.605Z&enddatetime=2020-01-30T12:30:13.605Z");
                response = await Get($"https://graph.microsoft.com/v1.0/me/calendarview?{query}");

                FindCalendarEntryResponse calendarResponse = JsonConvert.DeserializeObject<FindCalendarEntryResponse>(response);

                //response = string.Format("Created event {0} on {1} between {2} and {3}", calendarResponse.subject, calendarResponse.start.dateTime.ToShortDateString(), calendarResponse.start.dateTime.Subtract(-obj.Offset).ToShortTimeString(), calendarResponse.end.dateTime.Subtract(-obj.Offset).ToShortTimeString());
                // assemble response TODO
                response = AssembleResponse(calendarResponse);

                result.ResultText = response;
                result.Code = APIResultCode.Ok;

                return result;
            }
            catch (Exception ex)
            {
                result.ResultText = "Error while creating event.";
                result.Code = APIResultCode.Error;
                result.ErrorText = ex.Message;

                return result;
            }

        }
        private string AssembleUrlQuery(Intent intent)
        {
            string dateFrom = "";
            string dateTo = "";

            var entities = intent.RequiredEntities;
            dateFrom = entities.First().ValueStr;
            dateTo = entities.Last().ValueStr;

            // include logic to determine times TODO


            string path = string.Format("startDateTime={0}&endDateTime={1}", dateFrom,dateTo);

            return path;
        }
        private string AssembleResponse(FindCalendarEntryResponse calendarResponse)
        {
            string text = "";

            if (calendarResponse == null)
                return "";

            if (calendarResponse.value == null)
                return "";

            if (calendarResponse.value.Count() == 0)
            {
                return "There are no events in this time range.";
            }

            if (calendarResponse.value.Count() > 1)
                text = "Found these events:";
            else
                text = "Found event:";

            text += System.Environment.NewLine;
            foreach (Value obj in calendarResponse.value)
            {
                if(obj.start.dateTime.DayOfYear == DateTime.UtcNow.DayOfYear)
                    text += String.Format("{0} at {1} till {2}",obj.subject,obj.start.dateTime.ToShortTimeString() ,obj.end.dateTime.ToShortTimeString());
                else
                    text += String.Format("{0} at {1} {2} till {3} {4}", obj.subject, obj.start.dateTime.ToShortDateString(), obj.subject, obj.start.dateTime.ToShortTimeString(), obj.end.dateTime.ToShortDateString(), obj.end.dateTime.ToShortTimeString());

                text += System.Environment.NewLine;
            }

            return text;
        }
    }
}
