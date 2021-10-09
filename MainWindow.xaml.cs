using HtmlAgilityPack;
using Scraper.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Scraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<BitmapImage> Images = new List<BitmapImage>();
        private List<string> TotalWords = new List<string>();
        private List<Words> Words = new List<Words>();
        private int ImageNumber = 0;
        private DispatcherTimer PictureTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TextLabel.Content = "URL to Scrape:";
            TextLabel.Background = Brushes.White;
            TextLabel.Foreground = Brushes.Black;

            var url = URL.Text;

            //add http if needed
            if (!(url.StartsWith("http://") || url.StartsWith("https://")))
            {
                url = "http://" + url;
            }

            //validate url
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                TextLabel.Content = "Please Enter a Valid URL";
                TextLabel.Background = Brushes.Red;
                TextLabel.Foreground = Brushes.White;
                return;
            }

            //reset
            Images.Clear();
            Words.Clear();
            ImageCount.Content = "";
            WordCount.Text = "";


            var wc = new WebClient();
            var html = FinalHtml.GetFinalHtml(url, 10);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imageNodes = doc.DocumentNode.SelectNodes("//img/@src").Where(x => x.Attributes["src"].Value.ToLower().Contains(".jpg") || x.Attributes["src"].Value.ToLower().Contains(".png")).ToList();

            var textNodes = doc.DocumentNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Text && n.ParentNode.Name != "script" && n.ParentNode.Name != "style").ToList();

            if (imageNodes.Any())
            {
                foreach (var node in imageNodes)
                {
                    if (node.Attributes["src"].Value.StartsWith("http://") || node.Attributes["src"].Value.StartsWith("https://"))
                    {
                        Images.Add(new BitmapImage(new Uri(node.Attributes["src"].Value)));
                    }
                    else
                    {
                        Images.Add(new BitmapImage(new Uri(url + node.Attributes["src"].Value)));
                    }
                }
            }

            if (textNodes.Any())
            {
                foreach (var strWord in textNodes.Where(textNode => !string.IsNullOrEmpty(textNode.InnerText)).Select(textNode => Regex.Replace(textNode.InnerText, "[^a-zA-Z0-9'. ]", " ")).Select(str => str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)).SelectMany(strWords => strWords))
                {
                    var encoded = WebUtility.HtmlEncode(strWord);
                    TotalWords.Add(encoded);
                    if(Words.Any(item => item.word == encoded))
                    {
                        var obj = Words.FirstOrDefault(x => x.word == encoded);
                        if (obj != null) obj.amount += 1;
                    }
                    else
                    {
                        var thisWord = new Words();
                        thisWord.word = encoded;
                        thisWord.amount = 1;
                        Words.Add(thisWord);
                    }
                }
            }
            
            if (Words.Any())
            {
                var sorted = Words.OrderByDescending(x => x.amount).ToList();

                WordCount.Text = TotalWords.Count + " Words Found on " + url;

                //This will create a custom datasource for the DataGridView.
                var transactionsDataSource = sorted.Select(x => new
                {
                    Word = x.word,
                    Count = x.amount
                }).Take(7).ToList();

                //This will assign the datasource.
                TopWords.ItemsSource = transactionsDataSource;
            }

            if (Images.Any())
            {
                ImageCount.Content = "Images Found: " + imageNodes.Count.ToString();

                //Display the first image.
                imgPicture.Source = Images[0];

                // Install a timer to show each image.
                PictureTimer.Interval = TimeSpan.FromSeconds(3);
                PictureTimer.Tick += Tick;
                PictureTimer.Start();
            }
            else
            {
                ImageCount.Content = "Images Found: 0";
            }
        }


        private void Tick(object sender, System.EventArgs e)
        {
            ImageNumber = (ImageNumber + 1) % Images.Count;
            ShowNextImage(imgPicture);
        }

        private void ShowNextImage(Image img)
        {
            const double transition_time = 0.9;
            Storyboard sb = new Storyboard();

            // ***************************
            // Animate Opacity 1.0 --> 0.0
            // ***************************
            DoubleAnimation fade_out = new DoubleAnimation(1.0, 0.0,
                TimeSpan.FromSeconds(transition_time));
            fade_out.BeginTime = TimeSpan.FromSeconds(0);

            // Use the Storyboard to set the target property.
            Storyboard.SetTarget(fade_out, img);
            Storyboard.SetTargetProperty(fade_out,
                new PropertyPath(Image.OpacityProperty));

            // Add the animation to the StoryBoard.
            sb.Children.Add(fade_out);


            // *********************************
            // Animate displaying the new image.
            // *********************************
            ObjectAnimationUsingKeyFrames new_image_animation =
                new ObjectAnimationUsingKeyFrames();
            // Start after the first animation has finisheed.
            new_image_animation.BeginTime = TimeSpan.FromSeconds(transition_time);

            // Add a key frame to the animation.
            // It should be at time 0 after the animation begins.
            DiscreteObjectKeyFrame new_image_frame =
                new DiscreteObjectKeyFrame(Images[ImageNumber], TimeSpan.Zero);
            new_image_animation.KeyFrames.Add(new_image_frame);

            // Use the Storyboard to set the target property.
            Storyboard.SetTarget(new_image_animation, img);
            Storyboard.SetTargetProperty(new_image_animation,
                new PropertyPath(Image.SourceProperty));

            // Add the animation to the StoryBoard.
            sb.Children.Add(new_image_animation);


            // ***************************
            // Animate Opacity 0.0 --> 1.0
            // ***************************
            // Start when the first animation ends.
            DoubleAnimation fade_in = new DoubleAnimation(0.0, 1.0,
                TimeSpan.FromSeconds(transition_time));
            fade_in.BeginTime = TimeSpan.FromSeconds(transition_time);

            // Use the Storyboard to set the target property.
            Storyboard.SetTarget(fade_in, img);
            Storyboard.SetTargetProperty(fade_in,
                new PropertyPath(Image.OpacityProperty));

            // Add the animation to the StoryBoard.
            sb.Children.Add(fade_in);

            // Start the storyboard on the img control.
            sb.Begin(img);
        }
    }
}
