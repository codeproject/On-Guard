﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAAI.Properties;

using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet;
using System.Threading;

namespace SAAI
{
  public class MQTTPublish : IDisposable
  {
    static MqttClient s_client;
    static ManualResetEvent s_Connected = new ManualResetEvent(false);
    static ManualResetEvent s_ready = new ManualResetEvent(false);
    private bool disposedValue;
    static private int _retryDelay = 0;

    static MQTTPublish()
    {
#pragma warning disable CS4014 
      CreateClient();
#pragma warning restore CS4014 
    }


    public static async Task CreateClient()
    {
      var factory = new MqttFactory();
      s_client = (MqttClient)factory.CreateMqttClient();
      await Connect();

    }

    public static async Task Publish(string camera, AreaOfInterest area, Frame frame)
    {
      s_ready.WaitOne(10000);  // wait for the first connection attempt to complete (one way or the other) since the connection process is async
      if (s_client.IsConnected == false)
      {
        Dbg.Write("MQTTPublish - Client NotConnected");
      }
      else
      {

        string baseTopic = "OnGuard/";
        baseTopic += camera + "/";
        baseTopic += area.AOIName + "/";

        foreach (InterestingObject io in frame.Interesting)
        {
          string topic = baseTopic + io.FoundObject.Label;

          var message = new MqttApplicationMessageBuilder()
                  .WithTopic(topic)
                  .WithPayload(frame.Item.PendingFile)
                  .WithExactlyOnceQoS()
                  .WithRetainFlag()
                  .Build();

          await s_client.PublishAsync(message).ConfigureAwait(false);

        }
      }
    }



    public static async Task Connect()
    {

      string clientId = "OnGuard";
      string mqttURI = Settings.Default.MQTTServerAddress;
      string mqttUser = Settings.Default.MQTTUser;
      string mqttPassword = Settings.Default.MQTTPassword;
      int mqttPort = Settings.Default.MQTTPort;
      bool mqttSecure = Settings.Default.MQTTUseSecureLink;
    

      var messageBuilder = new MqttClientOptionsBuilder()
        .WithClientId(clientId)
        .WithCredentials(mqttUser, mqttPassword)
        .WithTcpServer(mqttURI, mqttPort)
        .WithCleanSession();

      var options = mqttSecure
        ? messageBuilder
          .WithTls()
          .Build()
        : messageBuilder
          .Build();


      _ = s_client.UseDisconnectedHandler(async e =>
        {
          Dbg.Write("MQTTPublish - Server Disconnected");

          if (_retryDelay > 0)
          {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
          }

          _retryDelay = 5;

          try
          {
            MqttClientAuthenticateResult disconnectRetryResult = await s_client.ConnectAsync(options).ConfigureAwait(false);
            if (disconnectRetryResult.ResultCode != MqttClientConnectResultCode.Success)
            {
              HandleError(disconnectRetryResult);
            }
            else
            {
              Dbg.Write("MQTTPublish - Connected succeeded after retry");
              s_ready.Set();
            }
          }
          catch (Exception ex)
          {
            Dbg.Write("MQTTPublish - Reconnection Failed: " + ex.Message);
          }
        });

      if (s_client.IsConnected == false)
      {
        try
        {
          MqttClientAuthenticateResult result = await s_client.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
          s_ready.Set();
          if (result.ResultCode == MqttClientConnectResultCode.Success)
          {
            Dbg.Write("MQTTPublish - Connected to server!");
          }
          else
          {
            HandleError(result);  // Here we let it go setup the disconnect handler -- things may improve in the future
          }
        }
        catch (Exception ex)
        {
          Dbg.Write("MQTTPublish - Connection to server failed: " + ex.Message);
          s_ready.Set();  // Well, it isn't "ready", but there is no sense waiting for a connection that will never come
          return;
        }
      }

    }

    static void HandleError(MqttClientAuthenticateResult result)
    {
      switch (result.ResultCode)
      {
        case MqttClientConnectResultCode.BadUserNameOrPassword:
          Dbg.Write("MQTTPublish - Connect - Invalid User Name or Password!");
          DisposeInstance();
          break;

        case MqttClientConnectResultCode.BadAuthenticationMethod:
          Dbg.Write("MQTTPublish - Connect - Bad authentication method - secure connection type invaid?  " + result.ResultCode.ToString());
          DisposeInstance();
          break;

        case MqttClientConnectResultCode.NotAuthorized:
          Dbg.Write("MQTTPublish - Connect - User Not Authorized: " + result.ResultCode.ToString());
          DisposeInstance();
          break;

        case MqttClientConnectResultCode.ServerUnavailable:
          Dbg.Write("MQTTPublish - Connect - The server is unavailable");
          break;

        default:
          Dbg.Write("MQTTPublish - Connect Error - Error Code: " + result.ResultCode.ToString());
          break;
      }

    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          s_client.Dispose();
        }

        disposedValue = true;
      }
    }

    private static void DisposeInstance()
    {
      if (null != s_client)
      {
        Dbg.Write("MQTTPublish - Disposing - Cannot Continue");
        s_client.Dispose();
        s_client = null;
      }
    }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
