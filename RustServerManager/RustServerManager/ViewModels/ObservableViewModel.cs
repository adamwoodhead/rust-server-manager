using RustServerManager.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RustServerManager.ViewModels
{
    public abstract class ObservableViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private List<CancellationTokenSource> TaskCancellationTokens { get; set; } = new List<CancellationTokenSource>();

        public void CancelTasks()
        {
            foreach (CancellationTokenSource cancellationTokenSource in TaskCancellationTokens)
            {
                cancellationTokenSource.Cancel();
            }
        }

        internal void ViewModelLoopTask(Action action, int msDelay = 250)
        {
            CancellationToken cancellationToken = NewTaskToken();

            if (msDelay > 0)
            {
                Task.Run(async () => {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        action.Invoke();
                        await Task.Delay(msDelay);
                    }
                });
            }
            else
            {
                Task.Run(() => {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        action.Invoke();
                    }
                });
            }
        }

        internal void ViewModelTask(Action action)
        {
            Task.Run(() => {
                action.Invoke();
            });
        }

        private CancellationToken NewTaskToken()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            TaskCancellationTokens.Add(cancellationTokenSource);
            return cancellationTokenSource.Token;
        }

        public ObservableViewModel()
        {

        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);

            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
        #endregion

        #region INotifyDataErrorInfo
        private ConcurrentDictionary<string, List<string>> _errors = new ConcurrentDictionary<string, List<string>>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            _errors.TryGetValue(propertyName, out List<string> errorsForName);
            return errorsForName;
        }

        public bool HasErrors
        {
            get { return _errors.Any(kv => kv.Value != null && kv.Value.Count > 0); }
        }

        public Task ValidateAsync()
        {
            return Task.Run(() => Validate());
        }

        private readonly object _lock = new object();

        public void Validate()
        {
            lock (_lock)
            {
                ValidationContext validationContext = new ValidationContext(this, null, null);
                List<ValidationResult> validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                foreach (KeyValuePair<string, List<string>> kv in _errors.ToList())
                {
                    if (validationResults.All(r => r.MemberNames.All(m => m != kv.Key)))
                    {
                        _errors.TryRemove(kv.Key, out List<string> outLi);
                        OnErrorsChanged(kv.Key);
                    }
                }

                IEnumerable<IGrouping<string, ValidationResult>> q = from r in validationResults from m in r.MemberNames group r by m into g select g;

                foreach (IGrouping<string, ValidationResult> prop in q)
                {
                    List<string> messages = prop.Select(r => r.ErrorMessage).ToList();

                    if (_errors.ContainsKey(prop.Key))
                    {
                        _errors.TryRemove(prop.Key, out List<string> outLi);
                    }
                    _errors.TryAdd(prop.Key, messages);
                    OnErrorsChanged(prop.Key);
                }
            }
        }
        #endregion

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public virtual void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides
    }
}
