﻿using Acr.UserDialogs;
using Hodlr.Interfaces;
using Hodlr.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;

namespace Hodlr.Pages
{
    public class MainPage : ContentPage
    {
        private Grid portfolioGrid;
        private Label userValueLabel;
        private Label cashValueLabel;
        private Label profitLabel;
        private Picker fiatPicker;
        private Picker sourcePicker;
        private bool loaded;
        private ListView listView;
        private double width = 0;
        private double height = 0;
        private ObservableCollection<WrappedCell<Transaction>> WrappedItems = new ObservableCollection<WrappedCell<Transaction>>();

        public MainPage()
        {
            Title = "To the Moon!";

            ToolbarItem refresh = new ToolbarItem
            {
                Icon = "ic_refresh_white_24dp",
                Priority = (int)ToolbarItemOrder.Primary
            };
            refresh.Clicked += Refresh_Clicked;
            ToolbarItems.Add(refresh);

            profitLabel = new Label
            {
                FontSize = 28,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Text = "Loading"
            };

            userValueLabel = new Label
            {
                FontSize = 25,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Text = "Loading"
            };

            cashValueLabel = new Label
            {
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Text = "Loading"
            };

            portfolioGrid = new Grid();
            portfolioGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            portfolioGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            portfolioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            portfolioGrid.Children.Add(new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Text = "Loading"
            },0,0);

            fiatPicker = new Picker
            {
                Title = "Choose Fiat Currency",
                HorizontalOptions = LayoutOptions.Start
            };
            fiatPicker.SelectedIndexChanged += FiatPicker_SelectedIndexChanged;

            sourcePicker = new Picker
            {
                Title = "Choose price source",
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };
            sourcePicker.ItemsSource = AppUtils.PriceSources;
            sourcePicker.SelectedIndexChanged += SourcePicker_SelectedIndexChanged;

            StackLayout pickerLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    fiatPicker,
                    sourcePicker
                }
            };

            Button addButton = new Button { Text = "Add Transaction" };
            addButton.Clicked += (a,b)=>{ Navigation.PushAsync(new AddTransactionPage()); };

            listView = new ListView
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill
            };
            listView.ItemsSource = WrappedItems;
            listView.HasUnevenRows = true;
            listView.VerticalOptions = LayoutOptions.FillAndExpand;
            listView.ItemTemplate = new DataTemplate(typeof(TransactionCell));
            listView.ItemSelected += ListView_ItemSelected;

            StackLayout profitSection = new StackLayout
            {
                Spacing = 15,
                Padding = new Thickness(30, 10),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = {
                    profitLabel,
                    userValueLabel,
                    portfolioGrid,
                    cashValueLabel,
                    pickerLayout
                }
            };

            StackLayout transactionsSection = new StackLayout
            {
                Spacing = 15,
                Padding = new Thickness(30, 10),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    new Label
                    {
                        HorizontalTextAlignment = TextAlignment.Center,
                        Text = "Transactions:",
                        HorizontalOptions = LayoutOptions.Center
                    },
                    listView,
                    addButton
                }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children =
                {
                    profitSection,
                    transactionsSection
                }
            };

            GetVals();
        }

        private void ShowUpdateHint()
        {
            int thisHintNum = 1;

            if(AppUtils.LastShownHint < thisHintNum)
            {
                DisplayAlert("NEW: Homescreen widget!", "Hodlr has been updated with a homescreen widget. Long press on your launcher's homescreen to add it.", "Sweet!");
                AppUtils.LastShownHint = thisHintNum;
                AppUtils.SaveCache();
            }
        }

        private void Refresh_Clicked(object sender, EventArgs e)
        {
            GetVals();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width != this.width || height != this.height)
            {
                this.width = width;
                this.height = height;

                ((StackLayout)Content).Orientation = (width > height)? StackOrientation.Horizontal : StackOrientation.Vertical;
            }
        }

        private async void GetVals()
        {
            loaded = false;
            while(!loaded)
            {
                UserDialogs.Instance.ShowLoading(title: "Getting Data");
                bool success = await AppUtils.RefreshVals(AppUtils.PriceSources[AppUtils.SourcePrefIndex]);
                UserDialogs.Instance.HideLoading();

                if (!success || AppUtils.FiatConvert == null)
                {
                    bool existingData = AppUtils.FiatConvert != null;

                    if (existingData)
                    {
                        await DisplayAlert("Error", "Error fetching latest price data. Using cached data.", "Got it");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Error fetching latest price data.", "Retry");
                        continue;
                    }
                }

                loaded = true;
            }

            SetupPicker();
            LoadTransactions();
            ShowUpdateHint();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if(loaded)
            {
                LoadTransactions();
            }
        }

        private void LoadTransactions()
        {
            var transactions = App.DB.GetTransactions().ToList();
            if (WrappedItems == null)
            {
                WrappedItems = new ObservableCollection<WrappedCell<Transaction>>
                (transactions.Select(item => new WrappedCell<Transaction>()
                {
                    Item = item,
                    IsSelected = false
                }).ToList());
            }
            else
            {
                WrappedItems.Clear();
                foreach(var tr in transactions)
                {
                    WrappedItems.Add(new WrappedCell<Transaction>()
                    {
                        Item = tr,
                        IsSelected = false
                    });
                }
            }
            
            UpdateLabels();
        }

        private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null) return;
            ((ListView)sender).SelectedItem = null;

            Transaction chosen = ((WrappedCell<Transaction>)e.SelectedItem).Item;

            var answer = await DisplayAlert("Delete Transaction", "Do you want to delete this transaction?", "Yes", "No");
            if (answer)
            {
                App.DB.DeleteTransaction(chosen.Id);
                LoadTransactions();
            }
        }

        private void SetupPicker()
        {
            string current = null;
            if(fiatPicker.Items?.Count > 0)
            {
                current = fiatPicker.Items[fiatPicker.SelectedIndex];
            }

            List<string> currs = AppUtils.GetCurrencies();

            if(currs != null)
            {
                fiatPicker.ItemsSource = currs;
            }

            current = (current != null)? current : AppUtils.FiatPref;
            fiatPicker.SelectedIndex = fiatPicker.Items.IndexOf(current);

            sourcePicker.SelectedIndex = AppUtils.SourcePrefIndex;
        }

        private void FiatPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fiatPicker.Items == null ||
                fiatPicker.SelectedIndex == -1 ||
                fiatPicker.SelectedIndex > fiatPicker.Items.Count) return;
            AppUtils.FiatPref = fiatPicker.Items[fiatPicker.SelectedIndex];
            AppUtils.SaveCache();
            UpdateLabels();
        }

        private void SourcePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            AppUtils.SourcePrefIndex = sourcePicker.SelectedIndex;
            AppUtils.SaveCache();
            GetVals();
        }

        private void UpdateLabels()
        {
            List<Transaction> transactions = App.DB.GetTransactions().ToList();

            HodlStatus status = HodlStatus.GetCurrent(transactions);

            portfolioGrid.Children.Clear();
            portfolioGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < AppUtils.CryptoCurrencies.Length; i++)
            {
                string key = AppUtils.CryptoCurrencies[i];
                portfolioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                portfolioGrid.Children.Add(new Label
                        {
                            HorizontalTextAlignment = TextAlignment.Center,
                            HorizontalOptions = LayoutOptions.CenterAndExpand,
                            Text = string.Format(
                                "{0:0.00000} {1}",
                                status.TotalCryptos[key],
                                key)
                        },i, 0);
                portfolioGrid.Children.Add(
                    new Label
                    {
                        HorizontalTextAlignment = TextAlignment.Center,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Text = "at " + AppUtils.GetMoneyString(
                                AppUtils.ConvertFiat(
                                    "USD",
                                    AppUtils.FiatPref,
                                    AppUtils.FiatConvert.UsdToCrypto[key]),
                                AppUtils.FiatPref)
                    }, i, 1);
            }

            cashValueLabel.Text = "Cashed out: " + AppUtils.GetMoneyString(status.FloatingFiat, AppUtils.FiatPref);
            userValueLabel.Text = AppUtils.GetMoneyString(status.CryptoFiatVal, AppUtils.FiatPref);

            string profLoss = (status.Profit >= 0) ? "Profit" : "Loss";
            string plusMinus = (status.Profit >= 0) ? "+" : "-";

            profitLabel.TextColor = (status.Profit >= 0) ? Color.ForestGreen : Color.IndianRed;
            profitLabel.Text = string.Format("{0}: {1} ({2}{3:0.0}%)", 
                profLoss,
                AppUtils.GetMoneyString(Math.Abs(status.Profit), AppUtils.FiatPref),
                plusMinus, 
                Math.Abs(status.PercentChange));

            // Update the user's widgets if they have any
            DependencyService.Get<IWidgetManager>().UpdateWidget(status.CryptoFiatVal, status.Profit, AppUtils.FiatPref);
        }
    }
}