﻿using System;
using System.Collections.Generic;
using DataCollection.DependencyServices;
using DataCollection.Entities;
using DataCollection.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Xamarin.Forms;
using System.Linq;
using Kalect.Services.Entities;

namespace Kalect.Demo
{
    public class InspectionDetail : ContentPage
    {
        void QuestionButton_Clicked(object sender, EventArgs e)
        {
            //for other buttons revert color back to Gray
            foreach (Button qbtn in questionNavigationButtonBarLayout.Children)
            {
                qbtn.BackgroundColor = Color.FromHex("#EAEAEA");
                qbtn.TextColor = Color.Black;
            }

            //set the select button color and load question
            Xamarin.Forms.Button btn = (Button)sender;
            btn.BackgroundColor = Color.FromHex("#3693FF");
            btn.TextColor = Color.White;
            formGroup = (FormGroup)btn.CommandParameter;
            LoadQuestions(formGroup);

        }


        void SaveToolbarItem_Clicked(object sender, EventArgs e)
        {
            SaveAction();
        }

        private void SaveAction()
        {
            //Read formData
            string formData = FormDataService.FormData;

            //Validate
            JObject fd = JObject.Parse(formData);
            IList<string> messages;
            JSchema schema = JSchema.Parse(ValidationSchema);
            bool valid = fd.IsValid(schema, out messages);

            IList<string> errorMessages = messages.ToArray();
            List<string> errorMessageToDisplay = new List<string>();
            foreach (string errorMessage in errorMessages)
            {
                foreach (Component comp in formGroup.components)
                {
                    if (errorMessage.Contains(comp.path))
                    {
                        errorMessageToDisplay.Add(errorMessage);
                    }
                }

            }

            //Show Error
            lblErrorMessage.Text = string.Join(Environment.NewLine, errorMessageToDisplay.ToArray());

            //Save
            DependencyService.Get<IDataCollectionDependencyService>().SaveFormData(formData, AppDataWallet.SelectedAssessmentMetadata.AssessmentTrackingNumber.ToString(), SelectedFriendlyName, "FormData");

            if (errorMessageToDisplay.Count == 0)
            {
                var answer = DisplayAlert("Saved with No Errors", "Form Saved Succesfully", "OK");
            }
            else
            {
                var answer = DisplayAlert("Saved With Errors (" + errorMessageToDisplay.Count + ")", "Please check the Error messages for more details.", "OK");
            }
        }


        string SelectedFriendlyName;
        string ValidationSchema;
        FormInstance formInstance;
        FormService formService;
        StackLayout PageLayout;
        Label lblErrorMessage;
        FormGroup formGroup;
        ToolbarItem SaveToolbarItem;
        StackLayout questionNavigationButtonBarLayout;

        public InspectionDetail(Sections selectedSection)
        {
            Title = "Inspection Checklist";

            SelectedFriendlyName = selectedSection.SectionFriendlyName;
            formService = new FormService();

            CreateToolBar();
            CreateErrorLabel();

            //Get Form Instance
            formInstance = formService.GetFormInstance(AppDataWallet.SelectedAssessmentMetadata.AssessmentTrackingNumber.ToString(), selectedSection.SectionFriendlyName);
            ValidationSchema = formInstance.ValidationSchema;

            //generate Layout Dynamically
            PageLayout = new StackLayout();

            StackLayout formNameLayout = new StackLayout();
            formNameLayout.Padding = new Thickness(25, 25, 0, 0);
            Label lblFormName = new Label();
            lblFormName.Text = selectedSection.SectionDisplayName;
            lblFormName.TextColor = Color.FromHex("#000000");
            lblFormName.FontSize = 30;
            formNameLayout.Children.Add(lblFormName);
            PageLayout.Children.Add(formNameLayout);

            questionNavigationButtonBarLayout = new StackLayout();
            questionNavigationButtonBarLayout.Orientation = StackOrientation.Horizontal;
            questionNavigationButtonBarLayout.Padding = new Thickness(25, 10, 10, 10);
            PageLayout.Children.Add(questionNavigationButtonBarLayout);

            //Create Button list for Navigation
            Button questionButton;
            foreach (FormGroup fg in formInstance.FormModelView.formgroups)
            {
                questionButton = new Button();
                questionButton.WidthRequest = 80;
                questionButton.HeightRequest = 80;
                questionButton.CornerRadius = 40;
                questionButton.Margin = new Thickness(0,0, 20, 0);
                questionButton.BackgroundColor = Color.FromHex("#EAEAEA");
                questionButton.Text = fg.text;
                questionButton.TextColor = Color.Black;
                questionButton.Clicked += QuestionButton_Clicked;
                questionButton.CommandParameter = fg;
                questionNavigationButtonBarLayout.Children.Add(questionButton);

            }

            //Add Error Message
            PageLayout.Children.Add(lblErrorMessage);

            //Load First Question
            formGroup = formInstance.FormModelView.formgroups[0];
            LoadFirstQuestionByDefault(formGroup);

            ScrollView scrollView = new ScrollView();

            MessagingCenter.Subscribe<object, List<string>>(this, "PhotoMessageQuestion", async (sender, arg) =>
            {
                var photoAction = await DisplayActionSheet(arg[0], arg[1], arg[2],arg[3]);
                MessagingCenter.Send<object, string>(this, "PhotoMessageAnswer", photoAction);
            });

            //Final Page Content
            //Content = _pageLayout;
            //scrollView = new ScrollView();
            //Content = PageLayout;
            Content = new ScrollView
            {
                Content = PageLayout
            };

        }

        private void CreateToolBar()
        {
            SaveToolbarItem = new ToolbarItem();
            SaveToolbarItem.Text = "Save";
            SaveToolbarItem.Clicked += SaveToolbarItem_Clicked;
            ToolbarItems.Add(SaveToolbarItem);
        }

        private void CreateErrorLabel()
        {
            lblErrorMessage = new Label();
            lblErrorMessage.TextColor = Color.DarkRed;
        }

        private void LoadFirstQuestionByDefault(FormGroup fg)
        {
            //Select the color of first button
            View firstButton = questionNavigationButtonBarLayout.Children.FirstOrDefault<View>();
            Button frstbtn = (Button)firstButton;
            frstbtn.BackgroundColor  = Color.FromHex("#3693FF");
            frstbtn.TextColor = Color.White;
            //firstButton.SetBinding(Button.TextColorProperty, "#FFFFFF");


            LoadQuestions(fg);
        }

        private void LoadQuestions(FormGroup fg)
        {
            //Set static FormData
            FormDataService.FormData = formInstance.FormData;
            StackLayout formGroupLayout = new StackLayout();
            //_formGroupLayout.Children.Clear();
            formGroupLayout.Children.Add(formService.GenerateLayoutForSelectedFormGroup(formGroup, formInstance.FormData));

            //check if formGroupLayout has been added for previous question. Remove that add new one.
            if (PageLayout.Children.Count == 3)
            {
                PageLayout.Children.RemoveAt(2);
            }
            lblErrorMessage.Text = string.Empty;
            PageLayout.Children.Add(formGroupLayout);

        }
    }
}

