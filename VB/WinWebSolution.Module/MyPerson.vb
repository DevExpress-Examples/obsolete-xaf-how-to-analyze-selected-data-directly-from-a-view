Imports Microsoft.VisualBasic
Imports System

Imports DevExpress.Xpo

Imports DevExpress.ExpressApp
Imports DevExpress.Persistent.Base
Imports DevExpress.Persistent.BaseImpl
Imports DevExpress.Persistent.Validation

Namespace WinWebSolution.Module
	<DefaultClassOptions> _
	Public Class MyPerson
		Inherits Person
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub
	End Class

End Namespace
