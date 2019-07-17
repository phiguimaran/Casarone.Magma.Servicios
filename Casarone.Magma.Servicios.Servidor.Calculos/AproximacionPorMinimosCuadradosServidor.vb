Imports System.Drawing
Imports Magma.Tools.Core.Common.DataServices
Imports Magma.Tools.Core.Business.DataServices
Imports Magma.Tools.Extensiones.Common.Servicios.Interfaces
Imports Magma.Tools.Extensiones.Common.Servicios.Entities
Imports Magma.Tools.Extensiones.Common.Servicios.ResolverEntrada
Imports Magma.Tools.Core.Common.Security

Public Class AproximacionPorMinimosCuadradosServidor
    Implements IServiciosServidor
    Private _entradaresuelta As String
    Private _transaccion As System.Data.IDbTransaction

    Public Overloads Function Ejecutar(ByVal Entrada As Object, ByVal Transaccion As System.Data.IDbTransaction) As Object Implements IServiciosServidor.Ejecutar
        Dim result As Boolean
        Dim _cnxString As String
        Dim _cnxSql As System.Data.IDbConnection

        ' Si se llama desde un formulario, ya viene con la transacción del mismo
        ' Si la transacción está vacia es porque se llama de algun proceso no transaccional y hay que crear una y comitearla al final
        If Transaccion Is Nothing Then
            _cnxString = CType(System.Threading.Thread.CurrentPrincipal, MagmaPrincipal).StringConnection.Trim()
            _cnxSql = New System.Data.OleDb.OleDbConnection(_cnxString)

            If _cnxString.ToUpper.Contains("DNS=") Then
                _cnxSql = New System.Data.Odbc.OdbcConnection(_cnxString)
            ElseIf MagmaPrincipal.CurrentBackend() = BackendType.Informix Then
                _cnxSql = New System.Data.OleDb.OleDbConnection(_cnxString)
            Else
                _cnxSql = New System.Data.SqlClient.SqlConnection(_cnxString)
            End If
            _cnxSql.Open()
            _transaccion = _cnxSql.BeginTransaction()
            result = Ejecutar(Entrada)
            If result Then
                _transaccion.Commit()
                _cnxSql.Close()
                _cnxSql = Nothing
            Else
                _transaccion.Rollback()
                _cnxSql.Close()
                _cnxSql = Nothing
            End If
        Else
            _transaccion = Transaccion
            result = Ejecutar(Entrada)
        End If
        Return result
    End Function


    Public Function EntradaResuelta() As String Implements IServiciosServidor.EntradaResuelta
        Return _entradaresuelta
    End Function

    Private Function msgbox(ByVal msg As String) As Boolean
        Dim oex As New Exception(msg)
        Throw oex
    End Function

    Public Overloads Function Ejecutar(ByVal Entrada As Object) As Object Implements IServiciosServidor.Ejecutar
        ' Espero 2 parametros, el primero el numero de transaccion y el segundo el grado del polinomio a utilizar
        Dim gradoPolinomio As Integer
        Dim nrotrans As String
        Dim entradas As String()
        Dim tiempo As Double
        Dim lin As System.Data.DataTable
        Dim coef As List(Of Double)
        Dim listaDePuntos As New List(Of PointF)()
        Dim sqlParams As New SQLParameters(_transaccion)
        Dim x As New SQLCommand

        tiempo = Timer

        'cambia los @ de los parametros, por sus valores y devuelve todo el string separado por pipes
        _entradaresuelta = ResolverEntrada(Entrada, TipoFormato.gkintFormatoNO)
        entradas = _entradaresuelta.Split("|")
        nrotrans = entradas(0)
        ' controlar que exista la transaccion
        x.Text = "select count(nro_trans) from cpt_ajuhum_cab where nro_trans = '" & nrotrans & "'"
        If SQL.SelScalar(x, sqlParams) <> 1 Then
            msgbox("Transaccion no encontrada o duplicada.")
        End If
        ' obtener grado polinomio y controlar su validez
        x.Text = "select nvl(grado,0) from cpt_ajuhum_cab where nro_trans = '" & nrotrans & "'"
        gradoPolinomio = SQL.SelScalar(x, sqlParams)
        If gradoPolinomio < 1 Or gradoPolinomio > 6 Then
            msgbox("El grado del polinomio a obtener debe estar entre 1 y 6")
        End If
        ' Select into datatable retornando los puntos definidos
        x.Text = "select x_abscisa, y_ordenada from cpt_ajuhum_lin where nro_trans = '" & nrotrans & "'"
        lin = SQL.SelDataTable(x, sqlParams)
        If lin.Rows.Count < 3 Then
            msgbox("No se definieron suficientes puntos de control para la transaccion " & nrotrans)
        End If
        ' recorrer datatable y cargar lista necesaria para las funcionaes de calculo
        For i = 0 To lin.Rows.Count - 1
            listaDePuntos.Add(New PointF(lin.Rows(i)("x_abscisa"), lin.Rows(i)("y_ordenada")))
        Next
        ' calcular coeficientes
        coef = FindPolynomialLeastSquaresFit(listaDePuntos, gradoPolinomio)
        ' guardo los coeficientes obtenidos
        For i = 0 To coef.Count - 1
            x.Text = "update cpt_ajuhum_cab set coef" & i.ToString.Trim & " = " & coef(i).ToString.Trim & " where nro_trans = '" & nrotrans & "';"
            SQL.EjeCmd(x, sqlParams)
        Next

        Try
            ' eliminar tabla de humedad de esa trans
            ' insertar nueva tabla de humedad de esa trans
            tiempo = Timer - tiempo
        Catch
        End Try

        Return True

    End Function
    ' Find the least squares linear fit.
    Public Function FindPolynomialLeastSquaresFit(ByVal points As List(Of PointF), ByVal degree As Integer) As List(Of Double)
        ' Allocate space for (degree + 1) equations with 
        ' (degree + 2) terms each (including the constant term).
        Dim coeffs(degree, degree + 1) As Double

        ' Calculate the coefficients for the equations.
        For j As Integer = 0 To degree
            ' Calculate the coefficients for the jth equation.

            ' Calculate the constant term for this equation.
            coeffs(j, degree + 1) = 0
            For Each pt As PointF In points
                coeffs(j, degree + 1) -= Math.Pow(pt.X, j) * pt.Y
            Next pt

            ' Calculate the other coefficients.
            For a_sub As Integer = 0 To degree
                ' Calculate the dth coefficient.
                coeffs(j, a_sub) = 0
                For Each pt As PointF In points
                    coeffs(j, a_sub) -= Math.Pow(pt.X, a_sub + j)
                Next pt
            Next a_sub
        Next j

        ' Solve the equations.
        Dim answer() As Double = GaussianElimination(coeffs)

        ' Return the result converted into a List(Of Double).
        Return answer.ToList()
    End Function
    ' Perform Gaussian elimination on these coefficients.
    ' Return the array of values that gives the solution.
    Private Function GaussianElimination(ByVal coeffs(,) As Double) As Double()
        Dim max_equation As Integer = coeffs.GetUpperBound(0)
        Dim max_coeff As Integer = coeffs.GetUpperBound(1)
        For i As Integer = 0 To max_equation
            ' Use equation_coeffs(i, i) to eliminate the ith
            ' coefficient in all of the other equations.

            ' Find a row with non-zero ith coefficient.
            If (coeffs(i, i) = 0) Then
                For j As Integer = i + 1 To max_equation
                    ' See if this one works.
                    If (coeffs(j, i) <> 0) Then
                        ' This one works. Swap equations i and j.
                        ' This starts at k = i because all
                        ' coefficients to the left are 0.
                        For k As Integer = i To max_coeff
                            Dim temp As Double = coeffs(i, k)
                            coeffs(i, k) = coeffs(j, k)
                            coeffs(j, k) = temp
                        Next k
                        Exit For
                    End If
                Next j
            End If

            ' Make sure we found an equation with
            ' a non-zero ith coefficient.
            Dim coeff_i_i As Double = coeffs(i, i)
            If coeff_i_i = 0 Then
                Throw New ArithmeticException(String.Format(
                    "There is no unique solution for these points.",
                    coeffs.GetUpperBound(0) - 1))
            End If

            ' Normalize the ith equation.
            For j As Integer = i To max_coeff
                coeffs(i, j) /= coeff_i_i
            Next j

            ' Use this equation value to zero out
            ' the other equations' ith coefficients.
            For j As Integer = 0 To max_equation
                ' Skip the ith equation.
                If (j <> i) Then
                    ' Zero the jth equation's ith coefficient.
                    Dim coef_j_i As Double = coeffs(j, i)
                    For d As Integer = 0 To max_coeff
                        coeffs(j, d) -= coeffs(i, d) * coef_j_i
                    Next d
                End If
            Next j
        Next i

        ' At this point, the ith equation contains
        ' 2 non-zero entries:
        '      The ith entry which is 1
        '      The last entry coeffs(max_coeff)
        ' This means Ai = equation_coef(max_coeff).
        Dim solution(max_equation) As Double
        For i As Integer = 0 To max_equation
            solution(i) = coeffs(i, max_coeff)
        Next i

        ' Return the solution values.
        Return solution
    End Function
End Class

