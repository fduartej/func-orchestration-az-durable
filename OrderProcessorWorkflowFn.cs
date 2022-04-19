using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using azure_workflow_function.Models;
//using System.Data.SqlClient;
using System;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Data.SQLite;



namespace azure_workflow_function.Function
{
    public static class OrderProcessorWorkflowFn
    {

        [FunctionName("OrderProcessor_Starter")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            Order order = await req.Content.ReadAsAsync<Order>();
            string instanceId = await starter.StartNewAsync("OrderProcessorWorkflowFn", order);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("OrderProcessorWorkflowFn")]
        public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            Order order = context.GetInput<Order>();
            var paymentCompleted = await context.CallActivityAsync<bool>("CheckPaymentStatus", order.Id);
            if (paymentCompleted)
            {
                await context.CallActivityAsync("SendOrderToVendorQueue", order);
                await context.CallActivityAsync<bool>("SendConfirmationMail", order);
                return $"Order confirmation mail sent to {order.Email}";
            }
            else
            {
                await context.CallActivityAsync<bool>("SendCancellationMail", order);
                return $"Order is not completed. Cancellation mail sent to {order.Email}";
            }
        }  

        [FunctionName("CheckPaymentStatus")]
        public static async Task<bool> CheckPaymentStatus([ActivityTrigger] int orderId, ILogger log)
        {
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            using (SQLiteConnection connection = new SQLiteConnection(str))
            {
                connection.Open();
                var sql = $"select PaymentStatus from Payments where OrderId={orderId}";
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    object status = await command.ExecuteScalarAsync();
                    if (status != null)
                    {
                        string statusText = status.ToString();
                        if (statusText == "Completed") return true;
                        else return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        [FunctionName("SendOrderToVendorQueue")]
        [return: Queue("vendor-orders", Connection = "AzureWebJobsStorage")]
        public static string SendOrderToVendorQueue([ActivityTrigger] Order order, ILogger log)
        {
            return JsonConvert.SerializeObject(order);
        }   


        [FunctionName("SendConfirmationMail")]
        public static async Task<bool> SendConfirmationMail([ActivityTrigger] Order order, ILogger log)
        {
            try
            {
                var authKey = Environment.GetEnvironmentVariable("sendgrid_key");
                SendGridClient client = new SendGridClient(authKey);

                var from = new EmailAddress("fduartej@gmail.com", "byteSTREAM Admin");
                var subject = $"Your Order confirmed with order Id {order.Id}";
                var to = new EmailAddress(order.Email, order.CustomerName);
                var htmlContent = $"Hi {order.CustomerName},<br/>" +
                    $"Your order with Id {order.Id} for Rs {order.Amount}/- is confirmed by the seller. Your order will be " +
                    $"delivered on {order.DeliveryDate.ToShortDateString()}. ";
                var message = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return false;
            }
        }             


   [FunctionName("SendCancellationMail")]
        public static async Task<bool> SendCancellationMail([ActivityTrigger] Order order, ILogger log)
        {
            try
            {
                var authKey = Environment.GetEnvironmentVariable("sendgrid_key");
                SendGridClient client = new SendGridClient(authKey);

                var from = new EmailAddress("fduartej@gmail.com", "byteSTREAM Admin");
                var subject = $"Order cancelled. Order Id: {order.Id}";
                var to = new EmailAddress(order.Email, order.CustomerName);
                var htmlContent = $"Hi {order.CustomerName},<br/>" +
                    $"Your order with Id {order.Id} for Rs {order.Amount}/- is cancelled because the payment is " +
                    $"not completed. You can try to place the order after sometime.";
                var message = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return false;
            }
        }
    }
}