using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PassphraseGenerator
{
    public partial class MainWindow : Window
    {
        private List<string> wordList;
        private readonly Random random = new Random();
        private bool isInitializing = true; // Flag to prevent saving during initialization

        public MainWindow()
        {
            InitializeComponent();
            LoadWordList();
            LoadUserPreferences();
            ThemeToggle.IsChecked = IsDarkTheme;
            ApplyTheme(IsDarkTheme);
            isInitializing = false;
            GeneratePassphrase();
        }

        private void LoadUserPreferences()
        {
            try
            {
                // Load saved preferences
                WordCountSlider.Value = Properties.Settings.Default.WordCount;
                SeparatorComboBox.SelectedIndex = Properties.Settings.Default.SeparatorIndex;
                CapitalizeCheckBox.IsChecked = Properties.Settings.Default.Capitalize;
                AddNumberCheckBox.IsChecked = Properties.Settings.Default.AddNumber;
                AddSymbolCheckBox.IsChecked = Properties.Settings.Default.AddSymbol;
            }
            catch (Exception ex)
            {
                // If there's an error loading settings, use defaults
                MessageBox.Show($"Error loading preferences: {ex.Message}. Default settings will be used.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Set default values
                WordCountSlider.Value = 4;
                SeparatorComboBox.SelectedIndex = 0;
                CapitalizeCheckBox.IsChecked = false;
                AddNumberCheckBox.IsChecked = false;
                AddSymbolCheckBox.IsChecked = false;
            }
        }

        private void SaveUserPreferences()
        {
            // Skip saving during initialization
            if (isInitializing) return;

            try
            {
                // Save current preferences
                Properties.Settings.Default.WordCount = (int)WordCountSlider.Value;
                Properties.Settings.Default.SeparatorIndex = SeparatorComboBox.SelectedIndex;
                Properties.Settings.Default.Capitalize = CapitalizeCheckBox.IsChecked ?? false;
                Properties.Settings.Default.AddNumber = AddNumberCheckBox.IsChecked ?? false;
                Properties.Settings.Default.AddSymbol = AddSymbolCheckBox.IsChecked ?? false;

                // Save to disk
                Properties.Settings.Default.Save();

                // Update status
                StatusText.Text = "Settings saved at " + DateTime.Now.ToShortTimeString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preferences: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error saving settings";
            }
        }

        public bool IsDarkTheme
        {
            get { return Properties.Settings.Default.IsDarkTheme; }
            set
            {
                Properties.Settings.Default.IsDarkTheme = value;
                Properties.Settings.Default.Save();
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isDarkTheme = ThemeToggle.IsChecked ?? false;
            IsDarkTheme = isDarkTheme;
            ApplyTheme(isDarkTheme);

            // Update status
            StatusText.Text = $"Theme changed to {(isDarkTheme ? "Dark" : "Light")} mode";
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            // Get the current resource dictionary
            ResourceDictionary currentDict = Application.Current.Resources.MergedDictionaries[0];

            // Get the new theme
            string themePath = isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            ResourceDictionary newDict = new ResourceDictionary
            {
                Source = new Uri($"/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/{themePath}", UriKind.RelativeOrAbsolute)
            };

            // Replace the dictionary
            Application.Current.Resources.MergedDictionaries[0] = newDict;

            // Update window style
            Style windowStyle = (Style)newDict["BaseWindowStyle"];
            this.Style = windowStyle;

            // Update the strength indicator color based on current value
            UpdateStrengthIndicator(PassphraseTextBox.Text);
        }

        private void LoadWordList()
        {
            try
            {
                // Load embedded word list
                string resourceName = "PassphraseGenerator.wordlist.txt";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    wordList = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(w => w.Trim().ToLower())
                                     .Where(w => w.Length >= 3 && w.Length <= 8)
                                     .ToList();
                }

                // If word list is empty or not found, use a fallback list
                if (wordList == null || wordList.Count < 100)
                {
                    wordList = new List<string> {
                        "apple", "banana", "orange", "grape", "melon", "lemon", "cherry",
                        "house", "table", "chair", "window", "door", "floor", "ceiling",
                        "happy", "funny", "silly", "brave", "smart", "quiet", "loud",
                        "river", "ocean", "mountain", "forest", "desert", "island", "valley",
                        "music", "movie", "story", "picture", "painting", "dance", "song",
                        "coffee", "pizza", "burger", "pasta", "cheese", "bread", "butter",
                        "purple", "yellow", "orange", "green", "blue", "red", "pink",
                        "jump", "skip", "run", "walk", "swim", "climb", "dance"
                    };
                }
                // Log the dictionary size
                StatusText.Text = $"Loaded dictionary with {wordList.Count} words. The larger the dictionary, the stronger the passphrase.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading word list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Use fallback list
                wordList = new List<string> { "error", "loading", "word", "list", "please", "check", "application", "files" };
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            GeneratePassphrase();
            SaveUserPreferences();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(PassphraseTextBox.Text))
            {
                Clipboard.SetText(PassphraseTextBox.Text);
                MessageBox.Show("Passphrase copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Ask for confirmation
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset to defaults
                WordCountSlider.Value = 4;
                SeparatorComboBox.SelectedIndex = 0;
                CapitalizeCheckBox.IsChecked = false;
                AddNumberCheckBox.IsChecked = false;
                AddSymbolCheckBox.IsChecked = false;

                // Save defaults
                SaveUserPreferences();

                // Generate a new passphrase
                GeneratePassphrase();

                MessageBox.Show("Settings have been reset to defaults.",
                    "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void GeneratePassphrase()
        {
            if (wordList == null || wordList.Count == 0)
            {
                PassphraseTextBox.Text = "Error: Word list not loaded.";
                return;
            }

            // Get options
            int wordCount = (int)WordCountSlider.Value;
            bool capitalize = CapitalizeCheckBox.IsChecked ?? false;
            bool addNumber = AddNumberCheckBox.IsChecked ?? false;
            bool addSymbol = AddSymbolCheckBox.IsChecked ?? false;

            // Get separator
            string separator = "-";
            switch (SeparatorComboBox.SelectedIndex)
            {
                case 0: separator = "-"; break;
                case 1: separator = "."; break;
                case 2: separator = " "; break;
                case 3: separator = ""; break;
            }

            // Generate passphrase
            List<string> selectedWords = new List<string>();

            // Use cryptographically secure random for better security
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomNumber = new byte[4];

                for (int i = 0; i < wordCount; i++)
                {
                    rng.GetBytes(randomNumber);
                    int index = Math.Abs(BitConverter.ToInt32(randomNumber, 0)) % wordList.Count;
                    string word = wordList[index];

                    if (capitalize)
                        word = char.ToUpper(word[0]) + word.Substring(1);

                    selectedWords.Add(word);
                }
            }

            string passphrase = string.Join(separator, selectedWords);

            // Add a number if requested
            if (addNumber)
            {
                passphrase += random.Next(0, 100).ToString("00");
            }

            // Add a symbol if requested
            if (addSymbol)
            {
                char[] symbols = { '!', '@', '#', '$', '%', '&', '*', '?', '+', '=' };
                passphrase += symbols[random.Next(0, symbols.Length)];
            }

            PassphraseTextBox.Text = passphrase;

            // Calculate and update strength indicator
            UpdateStrengthIndicator(passphrase);
        }

        /// <summary>
        /// Calculates the entropy (in bits) of the current passphrase
        /// </summary>
        private double CalculateEntropy(string passphrase)
        {
            // If no passphrase, entropy is 0
            if (string.IsNullOrEmpty(passphrase))
                return 0;

            // Base entropy from the wordlist and number of words
            double wordlistSize = wordList.Count;
            int wordCount = (int)WordCountSlider.Value;

            // Base entropy (each word adds log2(wordlistSize) bits of entropy)
            double baseEntropy = wordCount * Math.Log2(wordlistSize);

            // Additional entropy from customizations
            double additionalEntropy = 0;

            // Check if capitalization is enabled
            if (CapitalizeCheckBox.IsChecked == true)
            {
                // Each word can be capitalized or not (adds 1 bit per word)
                additionalEntropy += wordCount * 1;
            }

            // Check if numbers are added
            if (AddNumberCheckBox.IsChecked == true)
            {
                // We add a two-digit number (0-99), which adds log2(100) bits
                additionalEntropy += Math.Log2(100);
            }

            // Check if symbols are added
            if (AddSymbolCheckBox.IsChecked == true)
            {
                // We add one of 10 symbols, which adds log2(10) bits
                additionalEntropy += Math.Log2(10);
            }

            // Return total entropy
            return baseEntropy + additionalEntropy;
        }

        /// <summary>
        /// Gets a human-readable strength rating based on entropy
        /// </summary>
        private string GetStrengthRating(double entropy)
        {
            if (entropy < 45)
                return "Weak";
            else if (entropy < 60)
                return "Moderate";
            else if (entropy < 80)
                return "Strong";
            else if (entropy < 100)
                return "Very Strong";
            else
                return "Extremely Strong";
        }

        /// <summary>
        /// Gets information about password cracking time based on entropy
        /// </summary>
        private string GetCrackTimeEstimate(double entropy)
        {
            // Assuming modern hardware can check ~1 trillion (10^12) passwords per second
            // for an online attack (much slower) or dedicated offline attack (faster)

            if (entropy < 40)
                return "Could be cracked quickly";
            else if (entropy < 60)
                return "Could take hours to months to crack";
            else if (entropy < 80)
                return "Could take years to crack";
            else if (entropy < 100)
                return "Could take centuries to crack";
            else
                return "Would take longer than the age of the universe to crack";
        }

        private void UpdateStrengthIndicator(string passphrase)
        {
            // Calculate entropy
            double entropy = CalculateEntropy(passphrase);

            // Update the entropy display
            EntropyValueText.Text = $"{entropy:F1} bits";

            // Update the strength rating
            string strengthRating = GetStrengthRating(entropy);
            StrengthRatingText.Text = strengthRating;

            // Update time to crack estimate
            CrackTimeText.Text = GetCrackTimeEstimate(entropy);

            // Map entropy to a 0-100 scale for the progress bar
            // We'll consider 128 bits as "maximum" security for our scale
            double strengthPercentage = Math.Min(100, (entropy / 128) * 100);
            StrengthIndicator.Value = strengthPercentage;

            // Set color based on entropy
            if (entropy < 45)
                StrengthIndicator.Foreground = new SolidColorBrush(Colors.Red);
            else if (entropy < 60)
                StrengthIndicator.Foreground = new SolidColorBrush(Colors.Orange);
            else
                StrengthIndicator.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            string infoMessage = "Entropy is a measure of password strength in bits.\n\n" +
                                "The calculation is based on:\n" +
                                "• The size of the word dictionary\n" +
                                "• The number of words in the passphrase\n" +
                                "• Additional variations (capitalization, numbers, symbols)\n\n" +
                                "Higher entropy means a stronger passphrase:\n" +
                                "• <45 bits: Weak (vulnerable to fast attacks)\n" +
                                "• 45-60 bits: Moderate (resistant to online attacks)\n" +
                                "• 60-80 bits: Strong (difficult for most attackers)\n" +
                                "• 80-100 bits: Very Strong (resistant to state-level attackers)\n" +
                                "• >100 bits: Extremely Strong (theoretically uncrackable)";

            MessageBox.Show(infoMessage, "About Entropy Calculation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreferenceControl_Changed(object sender, RoutedEventArgs e)
        {
            SaveUserPreferences();
        }

        // Override OnClosed to ensure preferences are saved when app closes
        protected override void OnClosed(EventArgs e)
        {
            SaveUserPreferences();
            base.OnClosed(e);
        }
    }
}
