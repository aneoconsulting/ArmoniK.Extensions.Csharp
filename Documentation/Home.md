# References

## [ArmoniK.DevelopmentKit.GridServer](ArmoniK.DevelopmentKit.GridServer.md)

### [Class `ArmonikDataSynapseClientService`](ArmoniK.DevelopmentKit.GridServer.md#armonikdatasynapseclientservice)

### Methods:

- [CreateSession](ArmoniK.DevelopmentKit.GridServer_methods.md#sessionid-createsessiontaskoptions-taskoptions--null)
- [OpenSession](ArmoniK.DevelopmentKit.GridServer_methods.md#void-opensessionstring-session)
- [WaitCompletion](ArmoniK.DevelopmentKit.GridServer_methods.md#void-waitcompletionstring-taskid)
- [GetResults](ArmoniK.DevelopmentKit.GridServer_methods.md#taskienumerabletuplestring-byte-getresultsienumerablestring-taskids)
- [SubmitTasks](ArmoniK.DevelopmentKit.GridServer_methods.md#ienumerablestring-submittasksienumerablebyte-payloads)
- [SubmitTasksWithDependencies](ArmoniK.DevelopmentKit.GridServer_methods.md#ienumerablestring-submittaskswithdependenciesstring-session-ienumerabletuplebyte-iliststring-payloadwithdependencies)
- [TryGetResult](ArmoniK.DevelopmentKit.GridServer_methods.md#byte-trygetresultstring-taskid)
- [CloseSession](ArmoniK.DevelopmentKit.GridServer_methods.md#void-closesession)
- [CancelSession](ArmoniK.DevelopmentKit.GridServer_methods.md#void-cancelsession)

---

### [Class `ServiceInvocationContext`](ArmoniK.DevelopmentKit.GridServer.md#serviceinvocationcontext)

### Methods:

- [IsEquals](ArmoniK.DevelopmentKit.GridServer_methods.md#boolean-isequalsstring-session)

---

## [ArmoniK.DevelopmentKit.GridServer.Client](ArmoniK.DevelopmentKit.GridServer.Client.md)

### [Class `IServiceInvocationHandler`](ArmoniK.DevelopmentKit.GridServer.Client.md#iserviceinvocationhandler)

### Methods:

- [HandleError](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#void-handleerrorserviceinvocationexception-e-string-taskid)
- [HandleResponse](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#void-handleresponseobject-response-string-taskid)

---

### [Class `Properties`](ArmoniK.DevelopmentKit.GridServer.Client.md#properties)

---

### [Class `Service`](ArmoniK.DevelopmentKit.GridServer.Client.md#service)

### Methods:

- [LocalExecute](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#object-localexecuteobject-service-string-methodname-object-arguments)
- [Execute](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#object-executestring-methodname-object-arguments)
- [Submit](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#void-submitstring-methodname-object-arguments-iserviceinvocationhandler-handler)
- [Dispose](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#void-dispose)
- [Destroy](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#void-destroy)
- [IsDestroyed](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#boolean-isdestroyed)

---

### [Class `ServiceFactory`](ArmoniK.DevelopmentKit.GridServer.Client.md#servicefactory)

### Methods:

- [CreateService](ArmoniK.DevelopmentKit.GridServer.Client_methods.md#service-createservicestring-servicetype-properties-props)

---

### [Class `ServiceInvocationException`](ArmoniK.DevelopmentKit.GridServer.Client.md#serviceinvocationexception)

---

## [ArmoniK.DevelopmentKit.WorkerApi.Common](ArmoniK.DevelopmentKit.WorkerApi.Common.md)

### [Class `AppsOptions`](ArmoniK.DevelopmentKit.WorkerApi.Common.md#appsoptions)

---


## [ArmoniK.DevelopmentKit.SymphonyApi](ArmoniK.DevelopmentKit.SymphonyApi.md)

### [Class `ServiceContext`](ArmoniK.DevelopmentKit.SymphonyApi.md#servicecontext)

---

### [Class `SessionContext`](ArmoniK.DevelopmentKit.SymphonyApi.md#sessioncontext)

---

### [Class `TaskContext`](ArmoniK.DevelopmentKit.SymphonyApi.md#taskcontext)

---

## [ArmoniK.DevelopmentKit.SymphonyApi.api](ArmoniK.DevelopmentKit.SymphonyApi.api.md)

### [Class `ServiceContainerBase`](ArmoniK.DevelopmentKit.SymphonyApi.api.md#servicecontainerbase)

### Methods:

- [OnCreateService](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-oncreateserviceservicecontext-servicecontext)
- [OnSessionEnter](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-onsessionentersessioncontext-sessioncontext)
- [OnInvoke](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#byte-oninvokesessioncontext-sessioncontext-taskcontext-taskcontext)
- [OnSessionLeave](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-onsessionleavesessioncontext-sessioncontext)
- [OnDestroyService](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-ondestroyserviceservicecontext-servicecontext)
- [SubmitTasks](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#ienumerablestring-submittasksienumerablebyte-payloads)
- [SubmitTasksWithDependencies](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#ienumerablestring-submittaskswithdependenciesienumerabletuplebyte-iliststring-payloadwithdependencies)
- [WaitForCompletion](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-waitforcompletionstring-taskid)
- [WaitForSubTasksCompletion](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-waitforsubtaskscompletionstring-taskid)
- [GetResult](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#byte-getresultstring-taskid)
- [Configure](ArmoniK.DevelopmentKit.SymphonyApi.api_methods.md#void-configureiconfiguration-configuration-idictionarystring-string-clientoptions)

---

## [ArmoniK.DevelopmentKit.SymphonyApi.Client](ArmoniK.DevelopmentKit.SymphonyApi.Client.md)

### [Class `ArmonikSymphonyClient`](ArmoniK.DevelopmentKit.SymphonyApi.Client.md#armoniksymphonyclient)

### Methods:

- [CreateSession](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#string-createsessiontaskoptions-taskoptions--null)
- [OpenSession](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#void-opensessionsessionid-session)
- [WaitSubtasksCompletion](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#void-waitsubtaskscompletionstring-parenttaskid)
- [WaitCompletion](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#void-waitcompletionstring-taskid)
- [GetResults](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#taskienumerabletuplestring-byte-getresultsienumerablestring-taskids)
- [SubmitTasks](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#ienumerablestring-submittasksienumerablebyte-payloads)
- [SubmitSubTasks](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#ienumerablestring-submitsubtasksstring-session-string-parenttaskid-ienumerablebyte-payloads)
- [SubmitTasksWithDependencies](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#ienumerablestring-submittaskswithdependenciesstring-session-ienumerabletuplebyte-iliststring-payloadwithdependencies)
- [SubmitSubtaskWithDependencies](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#string-submitsubtaskwithdependenciesstring-session-string-parentid-byte-payload-iliststring-dependencies)
- [SubmitSubtasksWithDependencies](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#ienumerablestring-submitsubtaskswithdependenciesstring-session-string-parenttaskid-ienumerabletuplebyte-iliststring-payloadwithdependencies)
- [TryGetResult](ArmoniK.DevelopmentKit.SymphonyApi.Client_methods.md#byte-trygetresultstring-taskid)

---

## [ArmoniK.DevelopmentKit.WorkerApi.Common](ArmoniK.DevelopmentKit.WorkerApi.Common.md)

### [Class `AppsOptions`](ArmoniK.DevelopmentKit.WorkerApi.Common.md#appsoptions)

---

