using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Shutdown"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        public const string StartCommand = "start";
        private IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            else if(message is Messages.InputError)
            {
                _consoleWriterActor.Tell(message as Messages.InputError);
            }

            GetAndValidateInput();
            
        }

        #region Internal methods
        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();
            if (string.IsNullOrEmpty(message))
            {
                Self.Tell(new Messages.InputError.NullInputError("No input received"));
            }
            else if(string.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                Context.System.Shutdown();
            }
            else
            {
                var valid = IsValid(message);
                if (valid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thanks, input is valid..."));
                    Self.Tell(new Messages.ContinueProcessing());
                }
                else
                {
                    Self.Tell(new Messages.InputError.ValidationError("Invalid: Input had an odd number of characters"));
                }

            }

        }
        private static bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }
        #endregion
    }
}