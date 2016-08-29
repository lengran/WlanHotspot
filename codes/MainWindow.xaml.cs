using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using NETCONLib;

namespace WlanHotspot
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private List<string> Adapters = new List<string>();
        private string sharedAdapter;
        //private string viturialAdapter;
        private double savedWidth, savedHeight;

        public MainWindow()
        {
            InitializeComponent();
            check();
            settingLoader();
            savedWidth = mainWindow.Width;
            savedHeight = mainWindow.Height;
        }

        private void click(object sender, RoutedEventArgs e)
        {
            string ssid = name.Text;
            string password = passwordBox.Password;

            if(name.Text.Length==0)
            {
                status.Text = "出错啦，热点名称不能为空哦。";
                return;
            }
            if (password.Length < 8)
            {
                status.Text = "出错啦，密码应大于8位。";
                return;
            }

            status.Text = "正在建立热点....";
            string output = cmdl("netsh wlan set hostednetwork mode=allow ssid=" + ssid + " key=" + password);
            output = cmdl("netsh wlan start hostednetwork");

            if ((output.IndexOf("已启动承载网络") > -1))
            {
                status.Text = "热点已启动！";
                name.IsEnabled = false;
                passwordBox.IsEnabled = false;
                comboBox.IsEnabled = false;
                button.IsEnabled = false;
                button1.IsEnabled = true;
                connectSharing(comboBox.SelectedItem.ToString());
            }
            else
                status.Text = "热点启动出错！";
        }

        private void hotspotoff(object sender, RoutedEventArgs e)
        {
            string output = cmdl("netsh wlan stop hostednetwork");
            string output1 = cmdl("netsh wlan set hostednetwork mode=disallow");

            if ((output.IndexOf("已停止承载网络") > -1) && (output1.IndexOf("承载网络模式已设置为禁止") > -1))
            {
                status.Text = "热点已关闭！";
                name.IsEnabled = true;
                passwordBox.IsEnabled = true;
                comboBox.IsEnabled = true;
                if (comboBox.SelectedIndex != -1)
                    button.IsEnabled = true;
                button1.IsEnabled = false;
                //disconnectSharing();                                  // Bug need fixing.
            }
            else
            {
                status.Text = "热点关闭出错！";
                if ((output.IndexOf("已停止承载网络") == -1) && (output1.IndexOf("承载网络模式已设置为禁止") == -1))
                    status.Text += "\n(错误代码:0)";
                else if ((output.IndexOf("已停止承载网络") == -1) && (output1.IndexOf("承载网络模式已设置为禁止") > -1))
                    status.Text += "\n(错误代码:1)";
                else if ((output.IndexOf("已停止承载网络") > -1) && (output1.IndexOf("承载网络模式已设置为禁止") == -1))
                    status.Text += "\n(错误代码:2)";
            }
        }

        private void check()
        {
            string output = cmdl("netsh wlan show hostednetwork");

            if ((output.IndexOf("不可用") + output.IndexOf("未启动")) > -1)
            {
                status.Text = "当前Wifi热点未启动。";
                button1.IsEnabled = false;
            }
            else if ((output.IndexOf("已启动")) > -1)
            {
                status.Text = "当前Wifi热点正在运行中。";
                name.IsEnabled = false;
                passwordBox.IsEnabled = false;
                comboBox.IsEnabled = false;
                button.IsEnabled = false;
            }
        }

        private void settingLoader()
        {
            try
            {
                using (StreamReader stgr = new StreamReader("settings.sth"))
                {
                    string line;
                    if ((line = stgr.ReadLine()) != null)
                        name.Text = line;
                    if ((line = stgr.ReadLine()) != null)
                        passwordBox.Password = line;
                    if ((line = stgr.ReadLine()) != null)
                        sharedAdapter = line;
                }
            }
            catch (Exception e)
            {
                status.Text += "\n未找到配置文件，请直接输入热点名，密码。";
            }
        }

        private void settingStorer()
        {
            try
            {
                using (StreamWriter stw = new StreamWriter("settings.sth"))
                {
                    stw.WriteLine(name.Text);
                    stw.WriteLine(passwordBox.Password);
                    if (sharedAdapter != null)
                        stw.WriteLine(sharedAdapter);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(mainWindow, "无法保存设置，设置未保存。", "设置保存出错");
            }
        }

        private void exiting(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settingStorer();
        }

        private string cmdl(string cmdstr)
        {
            Process hotspot = new Process();
            hotspot.StartInfo.FileName = "cmd.exe";
            hotspot.StartInfo.UseShellExecute = false;
            hotspot.StartInfo.RedirectStandardError = true;
            hotspot.StartInfo.RedirectStandardInput = true;
            hotspot.StartInfo.RedirectStandardOutput = true;
            hotspot.StartInfo.CreateNoWindow = true;
            hotspot.Start();

            hotspot.StandardInput.WriteLine(cmdstr + "&exit");
            hotspot.StandardInput.AutoFlush = true;

            string output = hotspot.StandardOutput.ReadToEnd();
            hotspot.Close();

            return output;
        }

        private void connectSharing(string connectionToShare)
        {
            NetSharingManager manager = new NetSharingManager();
            INetSharingEveryConnectionCollection connections = manager.EnumEveryConnection;
            foreach (INetConnection c in connections)
            {
                INetConnectionProps props = manager.NetConnectionProps[c];
                INetSharingConfiguration sharingCfg = manager.INetSharingConfigurationForINetConnection[c];
                if (props.Name == connectionToShare)
                {
                    try
                    {
                        sharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);

                    }
                    catch (Exception e)
                    {
                        status.Text += "\n未能开启网络共享，可能热点无法联网……\n我也不知道为啥，不过有时候多重启几次就好了……";
                        return;
                    }
                    sharedAdapter = props.Name;
                }
                else if (props.DeviceName == "Microsoft Hosted Network Virtual Adapter")
                    sharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
            }
            status.Text += "\n网络已经共享，热点现在接入Internet啦。";
        }

        private void disconnectSharing()
        {
            NetSharingManager manager = new NetSharingManager();
            INetSharingEveryConnectionCollection connections = manager.EnumEveryConnection;

            foreach (INetConnection c in connections)
            {
                INetConnectionProps props = manager.NetConnectionProps[c];
                INetSharingConfiguration sharingCfg = manager.INetSharingConfigurationForINetConnection[c];
                if (sharingCfg.SharingEnabled)
                    sharingCfg.DisableSharing();                        // What's fucking wrong with this damn method!!!!!!!!!
            }

            status.Text += "\n网卡共享已经关闭啦。";
        }

        private void ChooseAdapter(object sender, EventArgs e)
        {
            Adapters = DetectAdapters();
            comboBox.ItemsSource = Adapters;
        }

        private void AdapterChoosed(object sender, EventArgs e)
        {
            if (comboBox.SelectedIndex != -1)
                button.IsEnabled = true;
        }

        private List<string> DetectAdapters()
        {
            List<string> myList = new List<string>();

            try
            {
                NetSharingManager managers = new NetSharingManager();
                INetSharingEveryConnectionCollection connections = managers.EnumEveryConnection;
                foreach (INetConnection item in connections)
                {
                    myList.Add(managers.NetConnectionProps[item].Name);
                }
                return myList;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private void changeSize(object sender, SizeChangedEventArgs e)
        {
            name.Width += (mainWindow.Width - savedWidth);
            passwordBox.Width += (mainWindow.Width - savedWidth);
            comboBox.Width += (mainWindow.Width - savedWidth);
            //button1.Margin = (90 + mainWindow.Width - 400).ToString() + ",110,0,0";
            status.Width += (mainWindow.Width - savedWidth);
            status.Height += (mainWindow.Height - savedHeight);
            savedHeight = mainWindow.Height;
            savedWidth = mainWindow.Width;
        }
    }
}
