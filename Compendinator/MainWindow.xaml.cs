using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.Windows.Controls.Ribbon;

namespace Compendinator {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow {
        #region URLs
        private const string URL_LOGIN = "http://www.wizards.com/dndinsider/compendium/login.aspx?page=monster&id=363";
        private const string URL_LIST_MONSTER = "http://www.wizards.com/dndinsider/compendium/CompendiumSearch.asmx/ViewAll?Tab=Monster";
        private const string URL_LIST_ITEM = "http://www.wizards.com/dndinsider/compendium/CompendiumSearch.asmx/ViewAll?Tab=Item";
        private const string URL_LIST_TRAP = "http://www.wizards.com/dndinsider/compendium/CompendiumSearch.asmx/ViewAll?Tab=Trap";
        private const string URL_LIST_RACE = "http://www.wizards.com/dndinsider/compendium/CompendiumSearch.asmx/ViewAll?Tab=Race";
        private const string URL_LIST_CLASS = "http://www.wizards.com/dndinsider/compendium/CompendiumSearch.asmx/ViewAll?Tab=Class";
        #endregion

        private string ViewState = "";
        private string EventValidation = "";
        private string CurrentStatusText = "";
        private string HiddenPassword = "";
        private bool SignedIn = false;

        public MainWindow() {
            InitializeComponent();
            // Insert code required on object creation below this point.
        }

        private void btnDDI_SignIn_Click(object sender, RoutedEventArgs e) {
            if (SignedIn) SignOut();
            else UpdateStupidData();
        }

        private void btnMonsters_RetrieveList_Click(object sender, RoutedEventArgs e) {
            Status("Retrieving monster list from DDI Compendium...");
            DisableAll();
            CustomWebClient client = new CustomWebClient();
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
            client.DownloadFileAsync(new Uri(URL_LIST_MONSTER), Directory.GetCurrentDirectory() + "\\monster_list.xml");
        }

        private void btnMonsters_ImportAll_Click(object sender, RoutedEventArgs e) {
            Log("Feature not implemented yet!");
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            if (CurrentStatusText == "") CurrentStatusText = statusBar.Text;
            statusBar.Text = CurrentStatusText + " (" + Convert.ToString(Convert.ToInt32(Math.Round((decimal)e.BytesReceived / (decimal)e.TotalBytesToReceive, 2) * 100)) + "%)";
            progressBar.Maximum = Convert.ToInt32(e.TotalBytesToReceive);
            progressBar.Value = Convert.ToInt32(e.BytesReceived);
        }
        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
            EnableAll();
            ProgressDone();
        }
        private void DownloadCompleted_StupidData(object sender, DownloadStringCompletedEventArgs e) {
            EnableAll();
            ProgressDone();
            Match m1 = Regex.Match(e.Result, "id=\"__VIEWSTATE\" value=\"(.*)\" />", RegexOptions.None);
            Match m2 = Regex.Match(e.Result, "id=\"__EVENTVALIDATION\" value=\"(.*)\" />", RegexOptions.None);
            if (m1.Success) {
                ViewState = m1.Groups[1].Value;
            }
            else Log("ERROR: Couldn't find view state string!");
            if (m2.Success) {
                EventValidation = m2.Groups[1].Value;
            }
            else Log("ERROR: Couldn't find event validation string!");

            Status("Attempting to sign into DDI...");
            string postdata = "__VIEWSTATE=" + HttpUtility.UrlEncode(ViewState) + "&__EVENTVALIDATION=" + HttpUtility.UrlEncode(EventValidation) + "&email=" + HttpUtility.UrlEncode(tbEmail.Text) + "&password=" + HttpUtility.UrlEncode(HiddenPassword) + "&InsiderSignin=Sign+In";
            DisableAll();
            CustomWebClient client = new CustomWebClient();
            client.UploadStringCompleted += new UploadStringCompletedEventHandler(UploadCompleted_SignIn);
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.UploadStringAsync(new Uri(URL_LOGIN), "POST", postdata);

        }
        private void UploadCompleted_SignIn(object sender, UploadStringCompletedEventArgs e) {
            ProgressDone();
            EnableAll();
            if (e.Result.Contains("Orcus")) SignInSuccess();
            else SignInFailure();
        }

        private void SignInSuccess() {
            Log("Successfully signed into DDI.");
            SignedIn = true;
            EnableDDIButtons();
        }
        private void SignInFailure() {
            Log("Failed to sign in to DDI! Double-check your e-mail address and password and/or hope that WotC didn't further gimp the API.");
        }
        private void SignOut() {
            DisableDDIButtons();
            SignedIn = false;
        }
        private void EnableDDIButtons() {
            btnMonsters_ImportAll.IsEnabled = true;
            btnItems_ImportAll.IsEnabled = true;
            btnTraps_ImportAll.IsEnabled = true;
            /*tbEmail.IsEnabled = false;
            tbPassword.IsEnabled = false;*/
            cbRemember.IsEnabled = false;
            btnDDI_SignIn.Label = "Sign Out";
        }
        private void DisableDDIButtons() {
            btnMonsters_ImportAll.IsEnabled = false;
            btnItems_ImportAll.IsEnabled = false;
            btnTraps_ImportAll.IsEnabled = false;
            /*tbEmail.IsEnabled = true;
            tbPassword.IsEnabled = true;*/
            cbRemember.IsEnabled = true;
            btnDDI_SignIn.Label = "Sign In";
        }

        private void UpdateStupidData() {
            Status("Obtaining view state and event validation strings from DDI...");
            DisableAll();
            CustomWebClient client = new CustomWebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadCompleted_StupidData);
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
            client.DownloadStringAsync(new Uri(URL_LOGIN));
        }
        private void CompareStupidData(string s) {
            string vs = "";
            string ev = "";
            Match m1 = Regex.Match(s, "id=\"__VIEWSTATE\" value=\"(.*)\" />", RegexOptions.None);
            Match m2 = Regex.Match(s, "id=\"__EVENTVALIDATION\" value=\"(.*)\" />", RegexOptions.None);
            if (m1.Success) {
                vs = m1.Groups[1].Value;
            }
            else MessageBox.Show("ERROR: Couldn't find view state string!", "Shite!", MessageBoxButton.OK, MessageBoxImage.Error);
            if (m2.Success) {
                ev = m2.Groups[1].Value;
            }
            else MessageBox.Show("ERROR: Couldn't find event validation string!", "Shite!", MessageBoxButton.OK, MessageBoxImage.Error);
            MessageBox.Show(vs + "\n" + ViewState + "\n" + ev + "\n" + EventValidation);
        }

        private void btnItems_RetrieveList_Click(object sender, RoutedEventArgs e) {
            Log("Feature not implemented yet!");
        }

        private void btnItems_ImportAll_Click(object sender, RoutedEventArgs e) {
            Log("Feature not implemented yet!");
        }

        private void btnTraps_RetrieveList_Click(object sender, RoutedEventArgs e) {
            Log("Feature not implemented yet!");
        }

        private void btnTraps_ImportAll_Click(object sender, RoutedEventArgs e) {
            Log("Feature not implemented yet!");
        }

        private void tbEmail_GotFocus(object sender, RoutedEventArgs e) {
            if (tbEmail.Text == "E-mail") tbEmail.Text = "";
        }
        private void tbEmail_LostFocus(object sender, RoutedEventArgs e) {
            if (tbEmail.Text == "") tbEmail.Text = "E-mail";
        }

        private void tbPassword_GotFocus(object sender, RoutedEventArgs e) {
            if (tbPassword.Text == "Password") tbPassword.Text = "";
            else tbPassword.Text = HiddenPassword;
        }
        private void tbPassword_LostFocus(object sender, RoutedEventArgs e) {
            if (tbPassword.Text == "") tbPassword.Text = "Password";
            else ObfuscatePassword();
        }

        private void Status(string s) {
            statusBar.Text = s;
            Log(s);
        }
        private void Log(string s) {
            tbConsole.Text += (tbConsole.Text != "" ? "\n" : "") + s;
            tbConsole.ScrollToEnd();
        }
        private void ProgressDone() {
            tbConsole.Text += " done.";
            tbConsole.ScrollToEnd();
            statusBar.Text = "";
            progressBar.Value = 0;
            CurrentStatusText = "";
        }

        private void DisableAll() {
            //RibbonWindow.IsEnabled = false;
        }
        private void EnableAll() {
            //RibbonWindow.IsEnabled = true;
        }

        private void ObfuscatePassword() {
            HiddenPassword = tbPassword.Text;
            string s = "";
            for (int i = 0; i < HiddenPassword.Length; i++) s += "*";
            tbPassword.Text = s;
        }

        private void cbRemember_Checked(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.RememberAuthInfo = true;
        }
        private void cbRemember_Unchecked(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.RememberAuthInfo = false;
        }

        private void tbEmail_TextChanged(object sender, TextChangedEventArgs e) {
            if (Properties.Settings.Default.RememberAuthInfo && RibbonWindow.IsLoaded) Properties.Settings.Default.Email = tbEmail.Text;
        }
        private void tbPassword_TextChanged(object sender, TextChangedEventArgs e) {
            if (Properties.Settings.Default.RememberAuthInfo && RibbonWindow.IsLoaded) Properties.Settings.Default.Password = HiddenPassword;
            string s = "";
            for (int i = 0; i < HiddenPassword.Length; i++) s += "*";
            if (RibbonWindow.IsLoaded && tbPassword.Text != s) HiddenPassword = tbPassword.Text;
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e) {
            DisableDDIButtons();
            if (Properties.Settings.Default.RememberAuthInfo) {
                tbEmail.Text = Properties.Settings.Default.Email;
                tbPassword.Text = Properties.Settings.Default.Password;
                ObfuscatePassword();
                cbRemember.IsChecked = true;
            }
            //dgMonsters.ItemsSource = LoadMonsters();
            LoadMonsters();
            //dgMonsters.ScrollIntoView(dgMonsters.Items[dgMonsters.Items.Count - 1]);
        }
        private void RibbonWindow_Closing(object sender, CancelEventArgs e) {
            Properties.Settings.Default.Save();
        }

        private void LoadMonsters() {
            string myXMLfile = Directory.GetCurrentDirectory() + "\\monster_list.xml";
        }

    }
    public class CustomWebClient : WebClient {

        private CookieContainer m_container = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address) {
            this.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest) {
                (request as HttpWebRequest).CookieContainer = m_container;
            }
            else MessageBox.Show(request.GetType().ToString());
            return request;
        }
    }
}
