﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RoomManegerApp;
using RoomManegerApp.Booking;

public class BookingInfo
{
    public string name { get; set; }
    public string phone { get; set; }
    public string email { get; set; }
    public string roomSize { get; set; }
    public string checkin { get; set; }
    public string checkout { get; set; }
    public string type { get; set; }
}

public class HttpServer
{
    private HttpListener _listener;

    public void Start()
    {
        if (_listener != null && _listener.IsListening) return;

        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:8080/booking/");
        _listener.Start();

        Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync();
            await ProcessRequest(context);
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod;
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        if (method == "OPTIONS")
        {
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            context.Response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            context.Response.StatusCode = 200;
            context.Response.Close();
            return;
        }
        if (method == "GET")
        {
            string message = "Server running!";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
            return;
        }
        if (method == "POST")
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var requestBody = await reader.ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<BookingInfo>(requestBody);

                if (data != null)
                {
                    string sql = @"insert into booking (name, phone, email, roomSize, checkin, checkout, type) values (@name, @phone, @email, @roomSize, @checkin, @checkout, @type)";
                    Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>()
                    {
                        { "@name", data.name},
                        { "@phone", data.phone},
                        { "@email", data.email},
                        { "@roomSize", data.roomSize},
                        { "@checkin", DateTime.Parse(data.checkin).ToString("yyyyMMdd")},
                        { "@checkout", DateTime.Parse(data.checkout).ToString("yyyyMMdd")},
                        { "@type", data.type},
                    });
                }

                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                var responseString = "Booking thành công!";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                var error = Encoding.UTF8.GetBytes("Lỗi: " + ex.Message);
                context.Response.StatusCode = 500;
                context.Response.ContentLength64 = error.Length;
                await context.Response.OutputStream.WriteAsync(error, 0, error.Length);
                context.Response.Close();
            }
        }
    }

    public void Stop()
    {
        if (_listener == null) return;

        _listener.Stop();
        _listener.Close();
        _listener = null;
    }
}