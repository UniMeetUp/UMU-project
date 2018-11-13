﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Input;
using CommonLib.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace UniMeetUpClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HubConnection connection;
        private const string storageDir = "ReceivedFiles";
        public MainWindow()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:44364/chatHub")
                .AddMessagePackProtocol()
                .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };

            Connect();
        }

        private async void Connect()
        {
            connection.On<string, string>("ReceiveMessage", (emailAddress, message) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    //String that are is displayed in listbox -> should be changed to displayname
                    var newMessage = $"{emailAddress}: {message}";
                    MessageList.Items.Add(newMessage);
                });
            });

            connection.On<FileMessage>("FileMessage", (file) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageList.Items.Add("File received");

                    Directory.CreateDirectory(storageDir);
                    File.WriteAllBytes(Path.Combine(storageDir, file.FileHeaders), file.FileBinary);
                    MessageList.Items.Add(file.FileHeaders);
                    //var bitmapImage = new BitmapImage();
                    //using (var ms = new MemoryStream(file.FileBinary))
                    //{
                    //    bitmapImage.BeginInit();
                    //    bitmapImage.StreamSource = ms;
                    //    bitmapImage.EndInit();
                    //}
                    //Image img = new Image();
                    //img.Source = bitmapImage;
                    //ChatHistory.Blocks.Add(new BlockUIContainer(img));
                });
            });

            try
            {
                await connection.StartAsync();
                MessageList.Items.Add("Connection started");
                //ConnectBtn.IsEnabled = false;
                SendBtn.IsEnabled = true;
            }
            catch (Exception exception)
            {
                MessageList.Items.Add(exception.Message);
            }
        }

        private async void SendBtnEvent(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async Task SendMessage()
        {
            try
            { 
                //Calls method in hub - with the three arguments: email, groupid and message
                await connection.InvokeAsync("SendMessage", "anne@petersen.dk", 1, MessageTextBox.Text);
                MessageTextBox.Clear();
                MessageTextBox.Focus();
            }
            catch (Exception exception)
            {
                MessageList.Items.Add(exception.Message);
            }
        }

        private async void SendFileBtnEvent(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string fullPath = openFileDialog.FileName;
                string fileName = Path.GetFileName(fullPath);
                FileMessage file = new FileMessage()
                {
                    FileBinary = File.ReadAllBytes(fullPath),
                    FileHeaders = fileName
                };
                try
                {
                    await connection.InvokeAsync("FileMessage", "anne@petersen.dk", 1, file);
                }
                catch (Exception exception)
                {
                    MessageList.Items.Add(exception.Message);
                }
            }
        }

        private void MessageTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
               SendMessage();
            }
        }
    }
}
