using System.Text;
using Akka.Actor;
using System.IO;

namespace WinTail
{
    class TailActor : UntypedActor
    {
        private FileObserver _observer;
        private Stream _fileStream;
        private StreamReader _fileStreamReader;

        #region Message Type
        public class FileWrite
        {
            public string Filename { get; private set; }
            public FileWrite(string filename)
            {
                Filename = filename;
            }
        }

        public class FileError
        {
            public string Filename { get; private set; }
            public string Reason { get; private set; }
            public FileError(string filename, string reason)
            {
                Filename = filename;
                Reason = reason;
            }
        }

        public class InitialRead
        {
            public string Filename { get; private set; }
            public string Text { get; private set; }
            public InitialRead(string filename, string text)
            {
                Filename = filename;
                Text = text;
            }
        }
        #endregion


        private readonly string _filepath;
        private readonly IActorRef _reporterActor;


        public TailActor(IActorRef reporterActor, string filepath)
        {
            _reporterActor = reporterActor;
            _filepath = filepath;
        }

        protected override void PreStart()
        {
            //start watching file for changes
            _observer = new FileObserver(Self, Path.GetFullPath(_filepath));
            _observer.Start();

            //open file stream to write to file while it is open
            _fileStream = new FileStream(Path.GetFullPath(_filepath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);


            //read initial file contents and send to console as first message
            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(_filepath, text));
        }


        protected override void PostStop()
        {
            _observer.Dispose();
            _observer = null;
            _fileStreamReader.Close();
            _fileStreamReader.Dispose();
            base.PostStop();
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                var fileError = message as FileError;
                _reporterActor.Tell(string.Format("Tail Error: {0}", fileError.Reason));
            }
            else if (message is InitialRead)
            {
                var initialRead = message as InitialRead;
                _reporterActor.Tell(initialRead.Text);
            }
        }


    }
}
