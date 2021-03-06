﻿
using System.IO;
using Akka.Actor;

namespace WinTail
{
    class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;
        

        public FileValidatorActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
            
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                _consoleWriterActor.Tell(new Messages.InputError.NullInputError("Blank input, type something mate..."));
                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                var valid = IsFileUri(msg);
                if (valid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess(string.Format("Start processing for {0}",msg)));
                    Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.InputError.ValidationError(string.Format("{0} is not a valid URI on disk", msg)));
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }
        }

        private static bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}
