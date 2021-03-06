﻿using RadialReview.Accessors;
using RadialReview.Models.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class TaskVM
    {
        public List<TaskModel> Tasks { get; set; }

        public String GetUrl(TaskModel task)
        {
            switch (task.Type)
            {
				case RadialReview.Models.Enums.TaskType.Review:		return "/Review/Take/" + task.Id;
				case RadialReview.Models.Enums.TaskType.Prereview:	return "/Prereview/Customize/" + task.Id;
				case RadialReview.Models.Enums.TaskType.Scorecard:	return "/Scorecard/Edit/";
				case RadialReview.Models.Enums.TaskType.Profile:	return "/Account/Manage";
				case RadialReview.Models.Enums.TaskType.Todo:		return "/Todo/List?todo="+task.Id;
                default: throw new ArgumentOutOfRangeException("TaskType is unknown (" + task.Type + ")");
            }
        }
    }

    public class TasksController : BaseController
    {
        //
        // GET: /Tasks/
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            var tasks= TaskAccessor.GetTasksForUser(GetUser(), GetUser().Id,DateTime.UtcNow);
            var model = new TaskVM()
            {
                Tasks=tasks
            };
            return View(model);
        }
	}
}