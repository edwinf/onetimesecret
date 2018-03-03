using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneTimeSecret.Web.Models.Status
{
    public class StatusItem
    {
        // Name of the item as you want it to appear in orion
        public string Name { get; set; }

        // generic message to display in orion when the status is down
        public string Message { get; set; }

        // UP or DOWN 
        public string Status { get; set; }

        // statistic to use in various message types.  Must be a decimal.  e.g. messages could trigger orion to look at this field for the number of messages.  DTU could look here for the value of the DTU count. 
        public string Statistic { get; set; }

        // response time to display / parse in orion.  Must be a decimal.  
        public string ResponseTime { get; set; }
    }
}
