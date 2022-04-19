using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;


namespace azure_workflow_function.Models
{
    public class Order
    {
        [JsonProperty("id")]        
        public int Id { get; set; }

        [JsonProperty("customerName")]        
        public string CustomerName { get; set; }

        [JsonProperty("amount")]        
        public double Amount { get; set; }

        [JsonProperty("orderDate")]        
        public DateTime OrderDate { get; set; }

        [JsonProperty("deliveryDate")]        
        public DateTime DeliveryDate { get; set; }

        [JsonProperty("email")]      
        public string Email { get; set; }
    }
}