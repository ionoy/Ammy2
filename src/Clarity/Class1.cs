using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Xamarin.Forms;

namespace Clarity
{
    public class MainScreen : ClarityBase
    {
        private List<Task> _tasks = new List<Task>();

        public BindableObject Build()
        {
            var maxTaskLength = 200;
            var taskModel = CreateBindableModel(new Task(), taskValidator);
            var text = CreateBindableValue("");
            
            void addTask()
            {
                _tasks.Add(taskModel.Value);
                taskModel.Value = new Task();
            }

            void taskValidator(Task task, Validator<Task> validator)
            {
                if (string.IsNullOrWhiteSpace(task.TaskName))
                    validator.AddError("Task name cannot be empty");
            }

            return Grid.Width(100)
                       .Height(200)
                       .Children(
                           Entry.Text(""),
                           Label.Text(text),
                           Label.Text(text, t => (maxTaskLength - t.Length) + " symbols left"),
                           Entry.Text(text, BindingMode.OneWay),
                           Entry.Text(taskModel.Get(m => m.TaskName)),
                           Label.Text(taskModel.Get(m => m.TaskName), t => (maxTaskLength - t.Length) + " symbols left"),
                           Button.Command(addTask)
                       );
        }
    }

    

    public class Task
    {
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; }
    }
}
