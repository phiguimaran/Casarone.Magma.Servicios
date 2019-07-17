Imports System.Drawing
Imports Magma.Tools.Extensiones.Common.Servicios.Interfaces
Imports Magma.Tools.Extensiones.Common.Servicios.Entities
Imports Magma.Tools.Extensiones.Common.Servicios.ResolverEntrada

Public Class AproximacionPorMinimosCuadrados
    Implements IServiciosServidor
    Private _entradaresuelta As String
    Private Const maximoGrado As Integer = 6
    Private Const minimaCantidadPuntos As Integer = 2

    Public Function EntradaResuelta() As String Implements IServiciosServidor.EntradaResuelta
        Return _entradaresuelta
    End Function

    Private Function msgbox(ByVal msg As String) As Boolean
        Dim oex As New Exception(msg)
        Throw oex
    End Function

    Public Overloads Function Ejecutar(ByVal Entrada As Object, ByVal Transaccion As System.Data.IDbTransaction) As Object Implements IServiciosServidor.Ejecutar
        ' Si se llama desde un formulario, ya viene con la transacción del mismo
        ' Si la transacción está vacia es porque se llama de algun proceso no transaccional y hay que crear una y comitearla al final
        Return Ejecutar(Entrada)
    End Function

    Public Overloads Function Ejecutar(ByVal Entrada As Object) As Object Implements IServiciosServidor.Ejecutar
        ' Espero 3 parametros, el grado del polinomio, una lista de valores de X y una lista de valores de Y
        ' retorna 7 valores correspondientes a los coeficientes de x^0 hasta x^6    (si el grado es menor 6 6 complementa con ceros)
        Dim gradoPolinomio As Integer
        Dim entradas As String()
        Dim abscisas As String()
        Dim ordenadas As String()
        Dim tiempo As Double
        Dim coef As List(Of Double)
        Dim listaDePuntos As New List(Of PointF)()
        Dim salida(maximoGrado, 0) As Object
        For i = 0 To maximoGrado
            salida(i, 0) = 0
        Next

        tiempo = Timer

        'cambia los @ de los parametros, por sus valores y devuelve todo el string separado por pipes
        _entradaresuelta = ResolverEntrada(Entrada, TipoFormato.gkintFormatoNO)
        entradas = _entradaresuelta.Split("|")
        gradoPolinomio = Convert.ToInt16(entradas(0))
        abscisas = entradas(1).Split(",")
        ordenadas = entradas(2).Split(",")
        ' obtener grado polinomio y controlar su validez
        If gradoPolinomio < 1 Or gradoPolinomio > maximoGrado Then
            msgbox("El grado del polinomio a obtener debe estar entre 1 y 6")
        End If
        ' Select into datatable retornando los puntos definidos
        If abscisas.Count < minimaCantidadPuntos Or ordenadas.Count < minimaCantidadPuntos Or abscisas.Count <> ordenadas.Count Then
            Return salida
        End If
        ' recorrer datatable y cargar lista necesaria para las funcionaes de calculo
        For i = 0 To abscisas.Count - 1
            listaDePuntos.Add(New PointF(Convert.ToDouble(abscisas(i)), Convert.ToDouble(ordenadas(i))))
        Next
        ' calcular coeficientes
        coef = FindPolynomialLeastSquaresFit(listaDePuntos, gradoPolinomio)
        ' guardo los coeficientes obtenidos
        For i = 0 To coef.Count - 1
            salida(i, 0) = coef(i)
        Next

        Try
            ' eliminar tabla de humedad de esa trans
            ' insertar nueva tabla de humedad de esa trans
            tiempo = Timer - tiempo
        Catch
        End Try

        Return salida

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

