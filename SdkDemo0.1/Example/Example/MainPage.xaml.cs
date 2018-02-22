using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Tego.Devices;
using Tego.Rfid.Gen2;
using Xamarin.Forms;

namespace Example
{
    public partial class MainPage : ContentPage
    {
        const uint PASSWORD_ZERO = 0;
        const int POPULATION = 50;
        
        private int asyncCount;

        #region Selection options

        const string SELECT_ALL = "All";
        const string SELECT_EPC = "EPC AAAAAAAAAAAAAAAAAAAAAAAA";
        const string SELECT_TEGO = "Tego chip";
        const string SELECT_UNPROGRAMMED_TEGO = "Tego chip with unprogrammed user memory";
        IList selectionOptions = new List<string> { SELECT_ALL, SELECT_EPC, SELECT_TEGO, SELECT_UNPROGRAMMED_TEGO };

        private ISelection GetSelection()
        {
            switch (selectionPicker.ItemsSource[selectionPicker.SelectedIndex])
            {
                case SELECT_ALL: return null;
                case SELECT_EPC: return new Select("AAAAAAAAAAAAAAAAAAAAAAAA");
                case SELECT_TEGO: return new Select(Bank.Tid, 9, new ushort[] { 0x02E0 }, 11); // Mfr mask X000 0001 0111, omitting X
                case SELECT_UNPROGRAMMED_TEGO:
                    return new Selection
                                                          {
                                                              new Select(Bank.Tid, 9, new ushort[] { 0x02E0 }, 11),
                                                              new Select(Bank.User, 0, new ushort[] { 0x0000 }, 16, SelectOperator.And)
                                                          };
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion Selection options


        #region Operation options

        const string OP_INVENTORY = "Search";
        const string OP_READ = "Simple read from user bank";
        const string OP_COMPOUND_READ = "Compound read from user and TID banks";

        IList operationOptions = new List<string> { OP_INVENTORY, OP_READ, OP_COMPOUND_READ };

        private IList<Operation> GetOperations()
        {
            switch (operationPicker.ItemsSource[operationPicker.SelectedIndex])
            {
                case OP_INVENTORY: return new List<Operation> { Operation.Search };
                case OP_READ: return new List<Operation> { new ReadOperation(Bank.User, 0, 4, PASSWORD_ZERO) };
                case OP_COMPOUND_READ:
                    return new List<Operation>
                    {
                        new ReadOperation(Bank.User, 0, 4, PASSWORD_ZERO),
                        new ReadOperation(Bank.Tid, 0, 3, PASSWORD_ZERO)
                    };
                // case OP_WRITE... Not implementing as a write combined with select all could overwrite a lot of tags accidentally
                    //return new WriteOperation(Bank.User, 0, writeDataWordArray, password);
                    //return new WriteOperation(Bank.User, 0, writeDataHex.ToWords(), password);
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion Operation options


        #region Execution settings options

        const string EXEC_1 = "One round";
        const string EXEC_5 = "Five rounds";
        const string EXEC_1S = "One second";
        const string EXEC_10_1S = "Ten rounds or one second";

        IList settingsOptions = new List<string> { EXEC_1, EXEC_5, EXEC_1S, EXEC_10_1S };

        private ExecutionSettings GetExecutionSettings()
        {
            switch (settingsPicker.ItemsSource[settingsPicker.SelectedIndex])
            {
                case EXEC_1: return new RoundsTrigger(1);
                case EXEC_5: return new RoundsTrigger(5);
                case EXEC_1S: return new TimedTrigger(1000);
                case EXEC_10_1S: return new TimedRoundsTrigger(1000, 10);
                default: throw new NotImplementedException();
            }
        }

        #endregion Execution settings options


        public MainPage()
        {
            InitializeComponent();

            selectionPicker.ItemsSource = selectionOptions;
            selectionPicker.SelectedIndex = 0;

            operationPicker.ItemsSource = operationOptions;
            operationPicker.SelectedIndex = 0;

            settingsPicker.ItemsSource = settingsOptions;
            settingsPicker.SelectedIndex = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // For demo convenience we'll attach a few event handlers when the page appears but if the page is going to
            // appear several times, we would want to avoid adding multiple redundant handlers for the same event
            if (App.Reader.Implementation is ITriggerProvider)
            {
                // If the reader has a trigger, then handle events when the user pulls or releases the trigger
                (App.Reader.Implementation as ITriggerProvider).Trigger += HandleTriggerEvent;
            }
            // The next two events are raised after StartExecution is called
            App.Reader.TagResponse += HandleAsynchronousTagResponse;
            App.Reader.ExecutionComplete += HandleAsynchronousExecutionComplete;
        }

        private void OnExecuteButtonClicked(object sender, EventArgs e)
        {
            asyncCountLabel.Text = string.Empty;
            // Note - real apps should execute in a task or different thread but invoke UI updates in the main thread
            // The next release of the API will provide an async Execute method or similar to simplify this
            // As our maximum run time on the demo is a second, we'll let execution block the UI thread for simplicity
            // here as we are demonstrating the reader API versus how to write responsive apps.
            Execute();
        }

        private void Execute()
        {
            var selection = GetSelection();
            var operations = GetOperations();
            var settings = GetExecutionSettings();

            var tagResponses = App.Reader.Execute(selection, operations, settings, POPULATION);

            var resultsList = new List<string>();
            foreach (var tagResponse in tagResponses)
            {
                Debug.WriteLine($"{tagResponse.EPC} seen with signal strength {tagResponse.Rssi}dBm");
                // The tagResponse has a lot of properties including a list of operation responses via tagResponse[]
                // but here we'll just use the ToString method as it gives a useful execution summary to output
                resultsList.Add(tagResponse.ToString());
            }
   
            listView.ItemsSource = resultsList;
        }

        private void OnStartExecutionButtonClicked(object sender, EventArgs e)
        {
            listView.ItemsSource = new string[0];
            asyncCount = 0;
            asyncCountLabel.Text = "Responses: 0";

            var selection = GetSelection();
            var operations = GetOperations();
            var settings = GetExecutionSettings();

            App.Reader.StartExecution(selection, operations, settings, POPULATION);
        }

        private void HandleAsynchronousTagResponse(object sender, TagResponseArgs args)
        {
            // Normally we will process the tagResponse (args.Response) but here we will just count the responses
            asyncCount++;
            // Unlike Execute which returns a list of combined tag responses to the caller thread when execution completes,
            // StartExecution raises TagResponse events in a background thread, so to update the UI with tag data (or in this
            // case a tag response cound) we must get back to the UI thread to update the UI so as to avoid a cross thread exception
            Device.BeginInvokeOnMainThread(() => asyncCountLabel.Text = $"Responses: {asyncCount}");
        }


        private void HandleAsynchronousExecutionComplete(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => asyncCountLabel.Text = $"Responses: {asyncCount} - execution complete");
        }

        private void HandleTriggerEvent(object sender, TriggerEventArgs args)
        {
            Debug.WriteLine($"Trigger {args.TriggerID}, pulled = {args.TriggerIsPulled}");
            // As in this simple app Execute updates the UI when done in the same thread, we need to make sure we execute
            // it in the UI thread to avoid a UI cross thread exception.
            if (args.TriggerIsPulled) Device.BeginInvokeOnMainThread(Execute);
        }

    }
}
