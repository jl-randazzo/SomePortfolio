using CWMasterTeacherDataModel;
using CWMasterTeacherDataModel.Interfaces;
using CWMasterTeacherDataModel.ObjectBuilders;
using CWMasterTeacherDomain.DomainObjects;
using CWMasterTeacherDomain.ViewObjects;
using CWMasterTeacherService.ViewObjectBuilder;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CWTesting.Tests.CWMasterTeacherDomain.ViewObjects
{
    [TestFixture]
    class DailyPlanningViewObjBuilderTest
    {
        Term _currentTerm;
        Term _lastTerm;
        List<Term> _termList;
        User _user0;
        List<User> _userList;
        Course _course0;
        Course _course1;
        List<Course> _courseList;
        ClassSection _classSection0;
        ClassSection _classSection1;
        ClassSection _classSection2;
        List<ClassSection> _classSectionList;
        ClassMeeting _classMeeting0;
        ClassMeeting _classMeeting1;
        ClassMeeting _classMeeting2;
        List<ClassMeeting> _classMeetingList;
        LessonUse _lessonUse0;
        LessonUse _lessonUse1;
        LessonUse _lessonUse2;
        List<LessonUse> _lessonUseList;
        Lesson _lesson0;
        Lesson _lesson1;
        List<Lesson> _lessonList;
        ReferenceCalendar _referenceCalendar0;
        ReferenceCalendar _referenceCalendar1;
        ReferenceCalendar _referenceCalendar2;
        List<ReferenceCalendar> _listReferenceCalendar;
        ClassSection _referenceSection0;
        ClassSection _referenceSection1;
        ClassSection _referenceSection2;
        List<ClassSection> _referenceSectionsForClassSection0;

        Mock<IClassSectionDomainObjBuilder> _classSectionBuilder;
        Mock<IClassMeetingDomainObjBuilder> _classMeetingBuilder;
        Mock<ICourseDomainObjBuilder> _courseBuilder;
        Mock<ILessonUseDomainObjBuilder> _lessonUseBuilder;
        Mock<ILessonDomainObjBuilder> _lessonBuilder;
        Mock<IUserDomainObjBuilder> _userBuilder;

        DailyPlanningViewObjBuilder _viewObjBuilder;

        [SetUp]
        public void Setup()
        {
            InitializeDbObjects();
            initializeDomainObjBuilderMocks();
            _viewObjBuilder = new DailyPlanningViewObjBuilder(_classSectionBuilder.Object, _classMeetingBuilder.Object, 
                                                              _courseBuilder.Object,_lessonUseBuilder.Object, 
                                                              _userBuilder.Object, _lessonBuilder.Object);
        }

        [Test]
        public void testRetrieveDailyPlanViewObjWithoutLocatableClassSection()
        {
            _user0.LastDisplayedCourseId = Guid.Empty;
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.Empty, _user0.Id, _lesson0.Id, 
                                                                                Guid.Empty, true, _lessonUse0.Id, 
                                                                                true, 1, Guid.Empty);

            // Checks that all class sections in all terms related to course are in the basic list
            List<Guid> classSectionIds = _classSectionList.Select(x => x.Id).ToList();
            foreach (var xClassSectionDomainObjBasic in viewObj.ClassSectionDomainObjBasicList)
                Assert.Contains(xClassSectionDomainObjBasic.Id, classSectionIds);

            Assert.AreEqual(_user0.Id, viewObj.CurrentUserObj.Id);
            Assert.AreEqual(Guid.Empty, viewObj.SelectedClassSectionId);
            Assert.AreEqual(_lessonUse0.Id, viewObj.SelectedLessonUseId);
        }

        [Test]
        public void testRetrieveDailyPlanViewObjWithClassSectionLocatedThroughClassMeeting()
        {
            _course0.LastDisplayedClassSectionId = _classSection1.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = Guid.Empty;

            // Since we can locate a ClassSection through the ClassMeeting Id passed as parameter, 
            // this is the first time we'll access the bulk of the method, hence the additional Asserts.
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.Empty, _user0.Id, _lesson0.Id, 
                                                                                Guid.Empty, false, _lessonUse0.Id, 
                                                                                false, 1, _classMeeting0.Id);

            // Checks that the setLastDisplayed methods worked correctly
            Assert.AreEqual(_course0.LastDisplayedLessonId, _lesson0.Id);
            Assert.AreEqual(_course0.LastDisplayedClassSectionId, _classSection0.Id);
            Assert.AreEqual(viewObj.SelectedLessonUseSequencNumber, _lessonUse0.SequenceNumber);
            Assert.AreEqual(viewObj.CourseObj.Id, _user0.LastDisplayedCourseId);

            // Checks that all class sections in the current term related to course are in the basic list, 
            // branch accessed by switching showAllTerms boolean to false
            List<Guid> classSectionIds = _classSectionList.Where(x => x.Course.Term.IsCurrent).Select(x => x.Id).ToList();
            foreach (var xClassSectionDomainObjBasic in viewObj.ClassSectionDomainObjBasicList)
                Assert.Contains(xClassSectionDomainObjBasic.Id, classSectionIds);

            //These lines assert the proper Guids exist in the regular select list
            List<String> guidList = _referenceSectionsForClassSection0.Select(x => x.Id.ToString()).ToList();
            Assert.Contains(viewObj.ReferenceCalendarClassSectionSelectList.ElementAt(0).Value, guidList);
            Assert.Contains(viewObj.ReferenceCalendarClassSectionSelectList.ElementAt(1).Value, guidList);

            Assert.AreEqual(_user0.Id, viewObj.CurrentUserObj.Id);
            Assert.AreEqual(_classSection0.Id, viewObj.SelectedClassSectionId);
            Assert.AreEqual(_lessonUse0.Id, viewObj.SelectedLessonUseId);
            Assert.AreEqual("Select Reference Calendar", viewObj.ReferenceCalDropdownHeading);

            //tests the booleans set using the Hash Set returned by the private method "GetUsedLessonIdSet"
            foreach (var xLessonDomainObj in viewObj.CourseObj.ContainerChildLessons)
            {
                Assert.True(xLessonDomainObj.IsForPlanning);
                Assert.True(xLessonDomainObj.IsUsedInPlan);
                Assert.Contains(xLessonDomainObj.Id, _lessonList.Select(x => x.Id).ToList());
            }
        }

        [Test]
        public void testRetrieveDailyPlanViewObjLocatingClassSectionThroughLastSelectedInCourseObj()
        {
            _course0.LastDisplayedClassSectionId = _classSection0.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = _lesson0.Id;

            // Any asserts that would have been repeats from the previous method have been omitted, 
            // we are only asserting new behaviors accessed by the different parameters
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.Empty, _user0.Id, Guid.Empty, 
                                                                                _classSection2.Id, false, Guid.Empty, 
                                                                                true, 2, Guid.Empty);

            Assert.AreEqual(_user0.Id, viewObj.CurrentUserObj.Id);
            Assert.AreEqual(_classSection0.Id, viewObj.SelectedClassSectionId);

            // These lines assert the proper Guids exist in the Possible select list, 
            // branch accessed through showAddReferenceCalendar boolean in parameter list being true
            List<String> guidList = new List<ClassSection>() { _classSection0, _classSection1 }
                                                             .Select(x => x.Id.ToString()).ToList();
            Assert.Contains(viewObj.ReferenceCalendarPossibleClassSectionSelectList.ElementAt(0).Value, guidList);
            Assert.Contains(viewObj.ReferenceCalendarPossibleClassSectionSelectList.ElementAt(1).Value, guidList);

            // Accessed by including a referenceClassSectionId, but the ClassSection has no assigned meetings, 
            // hence the empty list assertion
            Assert.AreEqual("Hide Reference Calendar", viewObj.ReferenceCalDropdownHeading);
            Assert.IsEmpty(viewObj.ReferenceCalendarClassMeetingObjList);
            Assert.AreEqual(_classSection2.Id, viewObj.ReferenceClassSectionId);
        }

        [Test]
        public void testRetrieveDailyPlanViewObjWithClassSectionId()
        {
            _course0.LastDisplayedClassSectionId = _classSection0.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = _lesson0.Id;
            
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(_classSection0.Id, _user0.Id, 
                                                                                Guid.Empty, _classSection0.Id, 
                                                                                false, Guid.Empty, true, 2, 
                                                                                Guid.Empty);

            /* This block reorders and then checks to make sure values were set correctly within the 
            *  SetClassMeetingValues method for the reference class section and it's class meeting domain obj's.
            *  The assertions are based on the paths that will be taken as a result of value definitions for the
            *  class meetingdbObjects contained in our setup
            */
            viewObj.ReferenceCalendarClassMeetingObjList = viewObj.ReferenceCalendarClassMeetingObjList
                                                                  .OrderBy(x => x.MeetingNumber).ToList();
            Assert.IsTrue(viewObj.ReferenceCalendarClassMeetingObjList.ElementAt(0).IsNextClass);
            Assert.AreEqual(viewObj.ReferenceCalendarClassMeetingObjList.ElementAt(1).ClassNumber, -1);
            Assert.AreEqual(viewObj.ReferenceCalendarClassMeetingObjList.ElementAt(2).WeekNumber, 1);
            foreach (var x in viewObj.ReferenceCalendarClassMeetingObjList) Assert.AreEqual(x.ClassCount, 2);

        }

        [Test]
        public void testMethodsWithUnrelatedClassSectionId()
        {
            _course0.LastDisplayedClassSectionId = _classSection0.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = _lesson0.Id;
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.NewGuid(), _user0.Id, Guid.Empty,
                                                                                _classSection0.Id, false, Guid.Empty, 
                                                                                true, 2, Guid.Empty);
            Assert.IsNull(viewObj.ClassSectionObj);
            ClassSectionDomainObj classSectionDomainObj = DailyPlanningViewObjBuilder
                                                          .SetClassMeetingValues(_classSectionBuilder.Object
                                                                                 .BuildFromId(Guid.NewGuid()), 
                                                                                 Guid.Empty, Guid.Empty);
            Assert.IsNull(classSectionDomainObj);
        }

        [Test]
        public void testRetrieveWithUnconnectedLessonUse()
        {
            _course0.LastDisplayedClassSectionId = _classSection0.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = _lesson0.Id;

            // This lesson use has no lesson object attached. We need this to test that the 
            // lastDisplayedLessonId isn't changed if the LessonUse is Unconnected to a Lesson
            LessonUse lessonUse = new LessonUse()
            {
                Id = Guid.NewGuid(),
                ClassMeetingId = Guid.NewGuid(),
                SequenceNumber = 42
            }; 

            _lessonUseList.Add(lessonUse);
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.Empty, _user0.Id, Guid.Empty,
                                                                                _classSection0.Id, false, lessonUse.Id,
                                                                                true, 2, Guid.Empty);
            Assert.AreEqual(viewObj.SelectedLessonUseSequencNumber, lessonUse.SequenceNumber);
            Assert.AreEqual(_course0.LastDisplayedLessonId, _lesson0.Id);
            _lessonUseList.Remove(lessonUse);
        }

        [Test]
        public void testRetrieveWithoutLastDisplayedLessonIdOrLessonUse()
        {
            _course0.LastDisplayedClassSectionId = _classSection0.Id;
            _user0.LastDisplayedCourseId = _course0.Id;
            _course0.LastDisplayedLessonId = Guid.Empty;
            DailyPlanViewObj viewObj = _viewObjBuilder.RetrieveDailyPlanViewObj(Guid.Empty, _user0.Id, Guid.Empty, 
                                                                                _classSection0.Id, false, Guid.Empty,
                                                                                true, 2, Guid.Empty);
            Assert.AreEqual(viewObj.CourseObj.SelectedLessonId, Guid.Empty);
        }

        private void InitializeDbObjects()
        {
            _currentTerm = new Term()
            {
                Id = Guid.NewGuid(),
                IsCurrent = true,
                StartDate = DateTime.Now
            };

            _lastTerm = new Term()
            {
                Id = Guid.NewGuid(),
                IsCurrent = false,
                StartDate = DateTime.Now.AddMonths(-6)
            };

            _termList = new List<Term>
            {
                _currentTerm,
                _lastTerm
            };

            _user0 = new User()
            {
                Id = Guid.NewGuid(),
                WorkingGroup = new WorkingGroup(),
                DisplayName = "test User"
            };

            _userList = new List<User>
            {
                _user0
            };

            _course0 = new Course()
            {
                Id = Guid.NewGuid(),
                UserId = _user0.Id,
                User = _user0,
                Term = _currentTerm,
                MetaCourseId = Guid.NewGuid(),
                CoursePreference = new CoursePreference() { Id = Guid.NewGuid() },
                ShowHiddenLessons = true
            };

            _course0.CoursePreferenceId = _course0.CoursePreference.Id;

            _course1 = new Course()
            {
                Id = Guid.NewGuid(),
                UserId = _user0.Id,
                User = _user0,
                Term = _lastTerm,
                MetaCourseId = _course0.MetaCourseId,
                CoursePreference = _course0.CoursePreference,
                CoursePreferenceId = _course0.CoursePreferenceId,
                ShowHiddenLessons = true
            };

            _courseList = new List<Course>()
            {
                _course0,
                _course1
            };

            _classSection0 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                CourseId = _course0.Id,
                Course = _course0
            };

            _classSection1 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                CourseId = _course0.Id,
                Course = _course0
            };

            _classSection2 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                CourseId = _course1.Id,
                Course = _course1
            };

            _classSectionList = new List<ClassSection>()
            {
                _classSection0,
                _classSection1,
                _classSection2
            };

            _classMeeting0 = new ClassMeeting()
            {
                Id = Guid.NewGuid(),
                ClassSectionId = _classSection0.Id,
                ClassSection = _classSection0,
                StartTime = DateTime.Now.AddDays(2),
                MeetingNumber = 1
            };

            _classMeeting1 = new ClassMeeting()
            {
                Id = Guid.NewGuid(),
                ClassSectionId = _classSection0.Id,
                ClassSection = _classSection0,
                NoClass = true, MeetingNumber = 2
            };

            _classMeeting2 = new ClassMeeting()
            {
                Id = Guid.NewGuid(),
                ClassSectionId = _classSection0.Id,
                ClassSection = _classSection0,
                IsBeginningOfWeek = true,
                MeetingNumber = 3
            };

            _classMeetingList = new List<ClassMeeting>()
            {
                _classMeeting0,
                _classMeeting1,
                _classMeeting2
            };

            _lesson0 = new Lesson()
            {
                Course = _course0,
                CourseId = _course0.Id,
                Id = Guid.NewGuid(),
                LessonPlan = new LessonPlan() { Id = Guid.NewGuid() },
                Narrative = new Narrative() { Id = Guid.NewGuid() }
            };

            _lesson0.LessonPlanId = _lesson0.LessonPlan.Id;
            _lesson0.NarrativeId = _lesson0.Narrative.Id;
            _lesson0.Narrative.Text = "Some Text";

            _lesson1 = new Lesson()
            {
                Course = _course0,
                CourseId = _course0.Id,
                Id = Guid.NewGuid(),
                Narrative = _lesson0.Narrative,
                NarrativeId = _lesson0.NarrativeId,
                LessonPlan = _lesson0.LessonPlan,
                LessonPlanId = _lesson0.LessonPlanId
            };

            _lessonList = new List<Lesson>()
            {
                _lesson0,
                _lesson1
            };

            _lessonUse0 = new LessonUse()
            {
                Lesson = _lesson0,
                LessonId = _lesson0.Id,
                Id = Guid.NewGuid(),
                SequenceNumber = 1,
                ClassMeeting = _classMeeting0,
                ClassMeetingId = _classMeeting0.Id
            };

            _lessonUse1 = new LessonUse()
            {
                Lesson = _lesson1,
                LessonId = _lesson1.Id,
                Id = Guid.NewGuid(),
                SequenceNumber = 2,
                ClassMeeting = _classMeeting0,
                ClassMeetingId = _classMeeting0.Id
            };

            _lessonUse2 = new LessonUse()
            {
                Lesson = _lesson1,
                LessonId = _lesson1.Id,
                Id = Guid.NewGuid(),
                SequenceNumber = 3,
                ClassMeeting = _classMeeting1,
                ClassMeetingId = _classMeeting1.Id
            };

            _lessonUseList = new List<LessonUse>()
            {
                _lessonUse0,
                _lessonUse1,
                _lessonUse2
            };

            _referenceSection0 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                Course = _course0,
                CourseId = _course0.Id
            };

            _referenceSection1 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                Course = _course0,
                CourseId = _course0.Id
            };

            _referenceSection2 = new ClassSection()
            {
                Id = Guid.NewGuid(),
                Course = _course1,
                CourseId = _course1.Id
            };

            _referenceCalendar0 = new ReferenceCalendar()
            {
                Id = Guid.NewGuid(),
                ClassSection = _classSection0,
                ClassSectionId = _classSection0.Id,
                ReferenceClassSection = _referenceSection0,
                ReferenceClassSectionId = _referenceSection0.Id
            };

            _referenceCalendar1 = new ReferenceCalendar()
            {
                Id = Guid.NewGuid(),
                ClassSection = _classSection0,
                ClassSectionId = _classSection0.Id,
                ReferenceClassSection = _referenceSection1,
                ReferenceClassSectionId = _referenceSection1.Id
            };

            _referenceCalendar2 = new ReferenceCalendar()
            {
                Id = Guid.NewGuid(),
                ClassSection = _classSection1,
                ClassSectionId = _classSection1.Id,
                ReferenceClassSection = _referenceSection2,
                ReferenceClassSectionId = _referenceSection2.Id
            };

            _listReferenceCalendar = new List<ReferenceCalendar>()
            {
                _referenceCalendar0,
                _referenceCalendar1,
                _referenceCalendar2
            };

            _referenceSectionsForClassSection0 = new List<ClassSection>()
            {
                _referenceSection0,
                _referenceSection1
            };

            _classSection0.ClassMeetings.Add(_classMeeting0);
            _classSection0.ClassMeetings.Add(_classMeeting1);
            _classSection0.ClassMeetings.Add(_classMeeting2);

            _classMeeting0.LessonUses = _lessonUseList;
            _classMeeting1.LessonUses = _lessonUseList;
            _classMeeting2.LessonUses = _lessonUseList;

            _course0.LastDisplayedClassSectionId = _classSection1.Id;
            _course0.ClassSections = new HashSet<ClassSection>()
            {
                _classSection0,
                _classSection1
            };
            _course0.Lessons.Add(_lesson0);
            _course0.Lessons.Add(_lesson1);
        }

        private void initializeDomainObjBuilderMocks()
        {
            _classSectionBuilder = new Mock<IClassSectionDomainObjBuilder>();
            _classMeetingBuilder = new Mock<IClassMeetingDomainObjBuilder>();
            _courseBuilder = new Mock<ICourseDomainObjBuilder>();
            _userBuilder = new Mock<IUserDomainObjBuilder>();
            _lessonBuilder = new Mock<ILessonDomainObjBuilder>();
            _lessonUseBuilder = new Mock<ILessonUseDomainObjBuilder>();

            _classSectionBuilder.Setup(mock => mock.BuildFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _classSectionList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return ClassSectionDomainObjBuilder.Build(x);
                        }
                    }

                    return null;
                }
            );

            _classSectionBuilder.Setup(mock => mock.ClassSectionsForUser(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    List<ClassSection> classSectionList = new List<ClassSection>();
                    foreach (var xClassSection in _classSectionList)
                    {
                        if (xClassSection.Course.User.Id.Equals(i))
                        {
                            classSectionList.Add(xClassSection);
                        }
                    }

                    return classSectionList.Count == 0 ? new List<ClassSectionDomainObjBasic>() : 
                    classSectionList.Select(x => ClassSectionDomainObjBuilder.BuildBasic(x)).ToList();
                }
            );

            _classSectionBuilder.Setup(mock => mock.ClassSectionsForUserAndCurrentTerm(It.IsAny<Guid>())).Returns(
               (Guid i) =>
               {
                   List<ClassSection> classSectionList = new List<ClassSection>();
                   foreach (var xClassSection in _classSectionList)
                   {
                       if (xClassSection.Course.User.Id.Equals(i) && xClassSection.Course.Term.IsCurrent)
                       {
                           classSectionList.Add(xClassSection);
                       }
                   }

                   return classSectionList.Count == 0 ? new List<ClassSectionDomainObjBasic>() : 
                   classSectionList.Select(x => ClassSectionDomainObjBuilder.BuildBasic(x)).ToList();
               }
           );

            _classSectionBuilder.Setup(mock => mock.ReferenceSectionsForClassSection(It.IsAny<Guid>())).Returns(
               (Guid i) =>
               {
                   List<ClassSectionDomainObjBasic> classSectionHits = new List<ClassSectionDomainObjBasic>();
                   foreach (var x in _listReferenceCalendar)
                   {
                       if (x.ClassSectionId.Equals(i))
                       {
                           classSectionHits.Add(ClassSectionDomainObjBuilder.BuildBasic(x.ReferenceClassSection));
                       }
                   }

                   return classSectionHits;
               }
           );

            _classSectionBuilder.Setup(mock => mock.PossibleReferenceClassSections(It.IsAny<Guid>(), 
                                                                                   It.IsAny<int>())).Returns(
               (Guid i, int j) =>
               {
                   List<ClassSectionDomainObjBasic> classSectionHits = new List<ClassSectionDomainObjBasic>();
                   _classSectionList = _classSectionList.OrderByDescending(x => x.Course.Term.StartDate).ToList();
                   foreach (var x in _classSectionList)
                   {
                       classSectionHits.Add(ClassSectionDomainObjBuilder.BuildBasic(x));
                   }

                   return classSectionHits.Take(j).ToList();
               }
           );

            _classMeetingBuilder.Setup(mock => mock.BuildFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _classMeetingList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return ClassMeetingDomainObjBuilder.Build(x);
                        }
                    }

                    return null;
                }
            );

            _courseBuilder.Setup(mock => mock.BuildFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _courseList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return CourseDomainObjBuilder.Build(x, x.ShowHiddenLessons, Guid.Empty, Guid.Empty, -1);
                        }
                    }

                    return null;
                }
            );

            _courseBuilder.Setup(mock => mock.GetLastDisplayedClassSectionId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    if(i.Equals(_user0.Id))
                    {
                        foreach (var xCourse in _courseList)
                        {
                            if (xCourse.Id.Equals(_user0.LastDisplayedCourseId))
                            {
                                return xCourse.LastDisplayedClassSectionId.Value;
                            }
                        }
                    }

                    return Guid.Empty;
                }
            );

            _courseBuilder.Setup(mock => mock.SetLastDisplayedClassSectionId(It.IsAny<Guid>())).Callback(
                (Guid i) =>
                {
                    foreach (var x in _classSectionList)
                    {
                        if (x.Id.Equals(i))
                        {
                            x.Course.LastDisplayedClassSectionId = x.Id;
                        }
                    }
                }
                );

            _courseBuilder.Setup(mock => mock.SetLastDisplayedLessonId(It.IsAny<Guid>())).Callback(
                (Guid i) =>
                {
                    foreach (var x in _lessonList)
                    {
                        if (x.Id.Equals(i))
                        {
                            x.Course.LastDisplayedLessonId = x.Id;
                        }
                    }
                }
                );

            _userBuilder.Setup(mock => mock.BuildFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _userList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return UserDomainObjBuilder.Build(x);
                        }
                    }

                    return UserDomainObjBuilder.Build(null);
                }
            );

            _lessonBuilder.Setup(mock => mock.BuildBasicFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _lessonList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return LessonDomainObjBuilder.BuildBasic(x);
                        }
                    }

                    return null;
                }
            );

            _lessonUseBuilder.Setup(mock => mock.BuildFromId(It.IsAny<Guid>())).Returns(
                (Guid i) =>
                {
                    foreach (var x in _lessonUseList)
                    {
                        if (i.Equals(x.Id))
                        {
                            return LessonUseDomainObjBuilder.Build(x);
                        }
                    }

                    return null;
                }
            );
        }
    }
}
