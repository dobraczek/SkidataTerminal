using System;
using System.Globalization;
using SkiData.ElectronicPayment;
using System.Diagnostics;
using System.Threading;

namespace Terminal
{
    public class Terminal : ITerminal3, IDisposable, ICardHandling2
    {
        private Settings _settings = new Settings();

        private TerminalConfiguration _terminalConfiguration;

        private bool _transactionResult;
        private bool _transactionCancelled = false;
        private bool _inTransaction = false;
        private bool _automatedCardHandling = true;
        private bool _activated = false;
        private int _cardRemovalDelay = 2000;
        private int _cardInsertionDelay = 1000;

        private bool disposed = false;


        private AutoResetEvent _transactionFinished;


        public Terminal()
        {

            #region Terminal settings
            this._settings.AllowsCancel = false;
            this._settings.AllowsCredit = false;
            this._settings.AllowsRepeatReceipt = false;
            this._settings.AllowsValidateCard = false;
            this._settings.AllowsVoid = false;
            this._settings.CanSetCardData = false;  
            this._settings.HasCardReader = true;     
            this._settings.IsContactless = true;
            this._settings.MayPrintReceiptOnRejection = true;  
            this._settings.NeedsSkidataChipReader = false;
            this._settings.RequireReceiptPrinter = true;    
            this._transactionFinished = new AutoResetEvent(initialState: false);

            #endregion
        }

        ~Terminal()
        {
            Dispose(disposing: false);
        }


        internal bool TransactionResult
        {
            set
            {
                _transactionResult = value;
                _transactionFinished.Set();
            }
        }


        #region ITerminal

        public string Name
        {
            get
            {
                return "SkiData.Terminal";
            }
        }

        public string ShortName
        {
            get
            {
                return "Terminal";

            }
        }


        public Settings Settings
        {
            get
            {
                return this._settings;
            }
        }

        public void AllowCards(CardIssuerCollection issuers)
        {
            OnTrace(TraceLevel.Verbose, "Terminal.AllowCards");

            foreach (CardIssuer cardIssuer in issuers)
            {
                this.OnTrace(TraceLevel.Verbose, "SCTerminal.AllowCards - issuer: {0}", cardIssuer.Abbreviation);
            }
        }

        public bool BeginInstall(TerminalConfiguration configuration)
        {
            this.OnTrace(TraceLevel.Verbose, "### Terminal.BeginInstall ###");
            this.OnTrace(TraceLevel.Verbose,
                "Terminal.BeginInstall - TerminalConfiguration\n" +
                "CommChannel  = {0}\n" +
                "Currency     = {1}\n" +
                "CustDisp.CPL = {2}\n" +
                "CustDisp.NOL = {3}\n" +
                "DeviceID     = {4}\n" +
                "DeviceName   = {5}\n" +
                "DeviceType   = {6}\n" +
                "OperDisp.CPL = {7}\n" +
                "OperDisp.NOL = {8}\n" +
                "Receipt.CPL  = {9}\n" +
                "TerminalID   = '{10}'\n",
                configuration.CommunicationChannel,
                configuration.Currency,
                configuration.CustomerDisplay.CharactersPerLine,
                configuration.CustomerDisplay.NumberOfLines,
                configuration.DeviceId,
                configuration.DeviceName,
                configuration.DeviceType.ToString(),
                configuration.OperatorDisplay.CharactersPerLine,
                configuration.OperatorDisplay.NumberOfLines,
                configuration.ReceiptConfiguration.CharactersPerLine,
                configuration.TerminalId);

            this._terminalConfiguration = configuration;

            return (true);
        }

        public void EndInstall()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.EndInstall");
            
        }
        public void Cancel()
        {
            this.OnTrace(TraceLevel.Verbose, "--- Terminal.Cancel ---");
            if (_inTransaction)
            {
                _transactionCancelled = true;
                _transactionFinished.Set();
            }
        }

        public TransactionResult Credit(TransactionData creditData)
        {

            this.OnTrace(TraceLevel.Verbose, "Terminal.Credit - not implemented");
            TransactionFailedResult txFailed = new TransactionFailedResult(TransactionType.Credit, DateTime.Now);
            txFailed.CustomerDisplayText = "";
            txFailed.OperatorDisplayText = "Not implemented";
            txFailed.ReceiptPrintoutMandatory = false;
            txFailed.Receipts = new Receipts();
            txFailed.RejectionCode = 23;
            return txFailed;
        }

        public TransactionResult Credit(TransactionData creditData, Card card)
        {

            this.OnTrace(TraceLevel.Verbose, "Terminal.Credit - not implemented");
            return this.Credit(creditData);
        }

        public TransactionResult Debit(TransactionData debitData)
        {
            return this.Debit(debitData, Card.NoCard);
        }


        public TransactionResult Debit(TransactionData debitData, Card card)
        {

            this.OnTrace(TraceLevel.Verbose, "Terminal.Debit Card");

            DisplayOperatorMessage("Inicjacja terminala");

            this.OnTrace(TraceLevel.Verbose, "Terminal.Debit");
            this.OnTrace(TraceLevel.Verbose, "Transaction amount: " + debitData.Amount);
            this.OnTrace(TraceLevel.Verbose, "Transaction reference: " + debitData.ReferenceId);
            this.OnTrace(TraceLevel.Verbose, "Transaction Client: " + debitData.ClientId);

            int amount = (int)(debitData.Amount * 100M);

            _inTransaction = true;

            //-----------------
            // Terminal
            //-----------------

            TransactionResult result;
            TransactionDoneResult transactionDoneResult = new TransactionDoneResult(TransactionType.Debit, DateTime.Now);
            transactionDoneResult.ReceiptPrintoutMandatory = false;
            transactionDoneResult.OperatorDisplayText = "Payment accepted";
            transactionDoneResult.AuthorizationNumber = "1234567890";
            transactionDoneResult.Amount = debitData.Amount;
            transactionDoneResult.Card.Number = "1234567890";
            transactionDoneResult.Card.Issuer = new CardIssuer("VISA_XXX");
            transactionDoneResult.TransactionNumber = debitData.ReferenceId;

            //transactionDoneResult.Receipts = GetReceipts(trx_details, debitData.Amount);

            result = transactionDoneResult;


            _transactionCancelled = false;
            _transactionResult = true;

            return (result);

        }

        public void Dispose()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.Dispose!!!!!!!!");
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            disposed = true;
        }



        public void ExecuteCommand(int commandId)
        {
            // no special action needed, just trace
            this.OnTrace(TraceLevel.Verbose, "Terminal.ExecuteCommand - commandType = {0}", commandId);
        }

        public void ExecuteCommand(int commandId, object parameterValue)
        {
            // no special action needed, just trace
            this.OnTrace(TraceLevel.Verbose, "Terminal.ExecuteCommand - commandType = {0}, parameter = {1}", commandId, parameterValue.ToString());
        }

        public void Notify(int notificationId)
        {
            // no special action needed, just trace
            this.OnTrace(TraceLevel.Verbose, "Terminal.Notify - notificationId = {0}", notificationId);
        }

        public CommandDefinitionCollection GetCommands()
        {
            //Not using
            this.OnTrace(TraceLevel.Verbose, "Terminal.CommandDefinitionCollection.GetCommands - not used now.");
            return new CommandDefinitionCollection();
        }

        public TransactionResult GetLastTransaction()
        {

            this.OnTrace(TraceLevel.Verbose, "Terminal.GetLastTransaction - not implemented/");
            TransactionResult trRes = new TransactionDoneResult(TransactionType.Debit, DateTime.Now);
            return trRes;

        }

        public Card GetManualCard(Card card)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.GetManualCard");
            return card;
        }

        public Card GetManualCard(Card card, string paymentType)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.GetManualCard + paymentType");
            return card;
        }

        public bool IsTerminalReady()
        {

            this.OnTrace(TraceLevel.Verbose, "Terminal.IsTerminalReady");
            return true;
        }



        public Card OpenInputDialog(IntPtr windowHandle, TransactionType transactionType, Card card)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.OpenInputDialog");
            return Card.NoCard;
        }

        public Receipts RepeatReceipt()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.RepeatReceipt");
            return new Receipts();
        }

        public void SetDisplayLanguage(CultureInfo cultureInfo)
        {

            this.OnTrace(TraceLevel.Verbose,
                "Terminal.SetDisplayLanguage - CultureInfo: {0:X4}  {1}",
                cultureInfo.LCID, cultureInfo.DisplayName);
        }

        public void SetParameter(Parameter parameter)
        {
            this.OnTrace(TraceLevel.Verbose,
                "Terminal.SetParameter - Parameter received: Key: '{0}', Value: '{1}'",
                parameter.Key, parameter.Value);
        }

        public bool SupportsCreditCards()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.SupportCreditCards - set to true.");
            return true;
        }

        public bool SupportsCustomerCards()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.SupportsCustomerCards - set to false.");
            return false;
        }

        public bool SupportsDebitCards()
        {
            throw new NotImplementedException();
        }

        public bool SupportsElectronicPurseCards()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.SupportElectronicPurseCards - set to true.");
            return true;
        }

        public ValidationResult ValidateCard(TransactionData transactionData)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.ValidateCard(TransactionData) - not implemented");
            ValidationResult result = new ValidationResult();
            result.RejectionCode = 0;
            return result;
        }

        public ValidationResult ValidateCard(TransactionData transactionData, Card card)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.ValidateCard(TransactionData, Card) - not implemented");
            ValidationResult result = new ValidationResult();
            result.RejectionCode = 0;
            return result;
        }

        public ValidationResult ValidateCard()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.ValidateCard - not implemented");
            ValidationResult result = new ValidationResult();
            result.RejectionCode = 0;
            return result;
        }

        public ValidationResult ValidateCard(Card card)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.ValidateCard(Card) - not implemented");
            ValidationResult result = new ValidationResult();
            result.RejectionCode = 0;
            return result;
        }

        public TransactionResult Void(TransactionDoneResult debitResultData)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.Void - set to failed");
            TransactionFailedResult txFailed = new TransactionFailedResult(TransactionType.Void, DateTime.Now);
            txFailed.CustomerDisplayText = "";
            txFailed.OperatorDisplayText = "Not implemented";
            txFailed.ReceiptPrintoutMandatory = false;
            txFailed.Receipts = new Receipts();
            txFailed.RejectionCode = 23;
            return txFailed;
        }

        #endregion

        #region ITerminal Events

        public event ActionEventHandler Action;
        public event ChoiceEventHandler Choice;
        public event ConfirmationEventHandler Confirmation;
        public event DeliveryCheckEventHandler DeliveryCheck;
        public event ErrorClearedEventHandler ErrorCleared;
        public event ErrorOccurredEventHandler ErrorOccurred;
        public event IrregularityDetectedEventHandler IrregularityDetected;
        public event EventHandler TerminalReadyChanged;
        public event JournalizeEventHandler Journalize;
        public event TraceEventHandler Trace;
        public event EventHandler CancelPressed;
        public event EventHandler CardInserted;
        public event EventHandler CardRemoved;

        protected void OnAction(ActionEventArgs args)
        {
            if (this.Action != null)
            {
                this.Action(this, args);
            }
        }

        protected void OnChoice(ChoiceEventArgs args)
        {
            if (Choice != null)
                Choice(this, args);
        }

        protected void OnConfirmation(ConfirmationEventArgs args)
        {
            if (Confirmation != null)
                Confirmation(this, args);
        }

        protected void OnDeliveryCheck(DeliveryCheckEventArgs args)
        {
            if (DeliveryCheck != null)
                DeliveryCheck(this, args);
        }

        protected void OnErrorOccurred(ErrorOccurredEventArgs args)
        {
            if (ErrorOccurred != null)
                ErrorOccurred(this, args);
        }

        protected void OnErrorCleared(ErrorClearedEventArgs args)
        {
            if (ErrorCleared != null)
                ErrorCleared(this, args);
        }

        protected void OnIrregularityDetected(IrregularityDetectedEventArgs args)
        {
            if (IrregularityDetected != null)
                IrregularityDetected(this, args);
        }

        protected void OnTerminalReadyChanged()
        {
            if (TerminalReadyChanged != null)
                TerminalReadyChanged(this, new EventArgs());
        }

        protected void OnJournalize(JournalizeEventArgs args)
        {
            if (Journalize != null)
                Journalize(this, args);
        }

        protected void OnTrace(TraceLevel level, string format, params object[] args)
        {
            if (Trace != null)
                Trace(this, new TraceEventArgs(String.Format(CultureInfo.InvariantCulture, format, args), level));
        }

        #endregion


        public void DisplayOperatorMessage(string message)
        {
            this.OnAction(new ActionEventArgs(message, ActionType.DisplayOperatorMessage));

        }


        public bool CardInTerminalAvailable
        {
            get;
            set;
        }

        internal void OnCancelPressed()
        {
            if (this.CancelPressed != null)
            {
                this.CancelPressed(this, new EventArgs());
            }
        }

        internal void OnCardRemoved()
        {
            if (this.CardRemoved != null)
            {
                this.OnTrace(TraceLevel.Verbose, "Terminal.OnCardRemoved");
                this.CardRemoved(this, new EventArgs());
            }
            this.CardInTerminalAvailable = false;
        }

        internal void OnCardInserted()
        {
            if (this.CardInserted != null)
            {
                this.CardInserted(this, new EventArgs());
                this.OnTrace(TraceLevel.Verbose, "Terminal.OnCardInserted");
            }
            this.CardInTerminalAvailable = true;
        }

        private static void CardRemovalThread(object stateInfo)
        {
            Terminal terminal = stateInfo as Terminal;
            terminal.OnTrace(TraceLevel.Verbose, "Terminal.CardRemovalThread");
            if (terminal != null)
            {
                Thread.Sleep(terminal._cardRemovalDelay);
                terminal.OnCardRemoved();
            }
        }

        private static void CardInsertionThread(object stateInfo)
        {
            Terminal terminal = stateInfo as Terminal;
            if (terminal != null)
            {
                terminal.OnTrace(TraceLevel.Verbose, "Terminal.CardInsertionThread.");
                Thread.Sleep(terminal._cardInsertionDelay);
                if (terminal._activated)
                {
                    terminal.OnTrace(TraceLevel.Verbose, "_CardInsertionThread Confirmation");
                    terminal.OnCardInserted();
                }
            }
        }



        #region ICardHandling3
        public void Activate(decimal amount)
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.Activate: {0}", new object[] { amount });
            this._activated = true;
            this.OnTrace(TraceLevel.Verbose, string.Concat("_transactionCancelled in Activate: ", this._transactionCancelled.ToString()));
            if (this._automatedCardHandling && !this._transactionCancelled && amount != decimal.Zero)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(Terminal.CardInsertionThread), this);
                return;
            }
            else
            {
                this.OnTrace(TraceLevel.Verbose, "Terminal.Activate - DO CancelPressed");
                this.OnCancelPressed();
                return;
            }
        }

        public void Deactivate()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.Deactivate");
            _activated = false;
            _transactionCancelled = false;
        }

        public void ReleaseCard()
        {
            this.OnTrace(TraceLevel.Verbose, "Terminal.ReleaseCard");
            if (this._settings.IsContactless)
            {
                this.OnCardRemoved();
                return;
            }
            ThreadPool.QueueUserWorkItem(new WaitCallback(Terminal.CardRemovalThread), this);
        }
        #endregion

    }
}
