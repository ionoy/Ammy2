using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Clarity
{
    class Workbench : ClarityPage
    {
        private List<MyTask> _tasks = new List<MyTask>();

        public override View BuildContent()
        {
            var maxTaskLength = 200;
            var taskModel = CreateBindableModel(new MyTask(), taskValidator);
            var text = CreateBindableValue("");
            
            void addTask()
            {
                _tasks.Add(taskModel.Value);
                taskModel.Value = new MyTask();
            }

            void taskValidator(MyTask task, Validator<MyTask> validator)
            {
                "".Equals("", StringComparison.InvariantCultureIgnoreCase);
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
    

    public class MyTask
    {
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; }
    }
}
