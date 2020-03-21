#ifndef LOWERLEVELLINEARFUNCTIONSTESTS_H
#define LOWERLEVELLINEARFUNCTIONSTESTS_H
#include<QObject>
#include<QtTest/QtTest>
#include<QTest>

class LinearFunctionsAndLogicalImageTests: public QObject{
    Q_OBJECT

private slots:
    void testVec3GetDirection();

    void testLogicalImage();

    void testMatrixMult();

    void testRowReduction();
    void testBSplineApproximation();

    void testMatrixRowsAndColumns();
};

#endif // LOWERLEVELLINEARFUNCTIONSTESTS_H
