#include "linearfunctionsandlogicalimagetests.h"
#include<../NormalTracker/LowerLevelLinearFunctions.h>
#include<../NormalTracker/UpperLinearFunctions.h>
#include<../NormalTracker/logicalimage.h>

void LinearFunctionsAndLogicalImageTests::testVec3GetDirection(){
    Vec3 vec(-1.1, -1, 0);
    auto list = vec.getDirectionHierarchy();
    auto it = list.begin();
    QVERIFY((*it).first == Direction::d_l);
    QVERIFY((*(++it)).first == Direction::l);
    QVERIFY((*(++it)).first == Direction::d);
    QVERIFY((*(++it)).first == Direction::u_l);
    QVERIFY((*(++it)).first == Direction::d_r);
    QVERIFY((*(++it)).first == Direction::u);
    QVERIFY((*(++it)).first == Direction::r);
    QVERIFY((*(++it)).first == Direction::u_r);
}

void LinearFunctionsAndLogicalImageTests::testLogicalImage(){
    QImage im = QImage(":/TestImages/frame1_depop.png");
    im.convertTo(QImage::Format::Format_Grayscale8);
    LogicalImage log_im(&im);
    log_im.writeColorsToNewImage();
    QImage dog = QImage(":/TestImages/frame4_depop.png");
    dog.convertTo(QImage::Format::Format_Grayscale8);
    LogicalImage log_im_2(&dog);
    log_im_2.writeColorsToNewImage();
    QImage circle = QImage(":/TestImages/circle_test.png");
    circle.convertTo(QImage::Format::Format_Grayscale8);
    LogicalImage log_im_3(&circle);
    log_im_3.writeColorsToNewImage();
}

void LinearFunctionsAndLogicalImageTests::testMatrixMult(){
    int identity[] {1, 0, 0,
                    0, 1, 0,
                    0, 0, 1 };
    LMat<int> mat_a(3, 3, identity);
    LMat<int> result = mat_a * mat_a;
    auto row_0 = mat_a.getRow(0);
    QVERIFY(row_0(0) == 1 && row_0(1) == 0 && row_0(2) == 0);
    auto row_1 = mat_a.getRow(1);
    QVERIFY(row_1(0) == 0 && row_1(1) == 1 && row_1(2) == 0);
    auto row_2 = mat_a.getRow(2);
    QVERIFY(row_2(0) == 0 && row_2(1) == 0 && row_2(2) == 1);

    int data_a[] {2, 4, // 2, 4, 5,
                 5, 2,  // 2, 1, 2
                 1, 2 };
    LMat<int> mat_b(2, 3, data_a);
    LMat<int> mat_c(3, 2, data_a);

    LMat<int> result_2 = mat_b * mat_c;
    auto row_3 = result_2.getRow(0);
    QVERIFY(row_3(0) == 29 && row_3(1) == 26);
    auto row_4 = result_2.getRow(1);
    QVERIFY(row_4(0) == 11 && row_4(1) == 14);
    QVERIFY(result_2.width == 2 && result_2.height == 2);

}

void LinearFunctionsAndLogicalImageTests::testMatrixRowsAndColumns(){
    int data[]{ 0, 1, 2,
                3, 4, 5,
                6, 7, 8 };
    LMat<int> mat_a(3, 3, data);
    AbsVecn<int> vec = mat_a.getRow(2);
    QVERIFY(vec(0) == 6 && vec(1) == 7 && vec(2) == 8);
    AbsVecn<int> vec_b = mat_a.getColumn(2);
    QVERIFY(vec_b(0) == 2 && vec_b(1) == 5 && vec_b(2) == 8);

    QVERIFY(vec.dot(vec_b) == 111);
}

void LinearFunctionsAndLogicalImageTests::testRowReduction(){
    int data[]{ 0, 1, 0,
                 0, 0, 1,
                 1, 0, 0 };
    LMat<int> data_mat(3, 3, data);
    int solve[]{ 2, 3, 4 };
    AbsVecn<int> solve_for(solve, 3, 1);
    LAugMat aug_mat(data_mat, solve_for);
    auto solution = dynamic_cast<AbsVecn<int>&>(aug_mat.rowReduction());
    QVERIFY(solution(0) == 4 && solution(1) == 2 && solution(2) == 3);
}

bool withinDelta(float expected, float actual){
    float diff = qAbs(expected - actual);
    return diff < .0001;
}

void LinearFunctionsAndLogicalImageTests::testBSplineApproximation(){
     auto bernstein_funcs = bernsteinPolynomials(2);
     double bernstein_data[18];
     for(int i = 0; i < 6; i++){
         double t_val = (double)i / (double)5;
         for(int j = 0; j < 3; j++){
             auto func = bernstein_funcs + j;
             bernstein_data[i * 3 + j] = (*func)(t_val);
         }
     }

     LMat<double> A(6, 3, bernstein_data);
     LMat A_t = A.transpose();
     LMat A_t_A = A_t * A;

     QVERIFY(withinDelta(A_t_A(0, 0), 1.5664) && withinDelta(A_t_A(0, 1), .4672) && withinDelta(A_t_A(0, 2), .1664));
     QVERIFY(withinDelta(A_t_A(1, 0), .4672) && withinDelta(A_t_A(1, 1), .6656) && withinDelta(A_t_A(1, 2), .4672));
     QVERIFY(withinDelta(A_t_A(2, 0), .1664) && withinDelta(A_t_A(2, 1), .4672) && withinDelta(A_t_A(2, 2), 1.5664));

     Vec3 points[6]{ Vec3(4,1, 0), Vec3(3, 2, 0), Vec3(3, 3, 0), Vec3(3, 4, 0), Vec3(4, 5, 0), Vec3(5, 6, 0) };
     AbsVecn<Vec3> b(points, 6, 1);
     AbsVecn<Vec3> A_t_b = A_t * b;

     QVERIFY(withinDelta(A_t_b(0).x, 7.64) && withinDelta(A_t_b(0).y, 4.2) && withinDelta(A_t_b(0).z, 0));

     LAugMat aug_mat(A_t_A, A_t_b);
     auto solution = dynamic_cast<AbsVecn<Vec3>&>(aug_mat.rowReduction());

     auto ident = dynamic_cast<const LMat<double>*>(aug_mat.getA());
     QVERIFY(withinDelta(1, (*ident)(0, 0)) && withinDelta(1, (*ident)(1, 1)) && withinDelta(1, (*ident)(2, 2)));
     QVERIFY(withinDelta(0, (*ident)(0, 1)) && withinDelta(0, (*ident)(0, 2)) && withinDelta(0, (*ident)(1, 0)));
     QVERIFY(withinDelta(0, (*ident)(1, 2)) && withinDelta(0, (*ident)(2, 0)) && withinDelta(0, (*ident)(2, 1)));

     QVERIFY(withinDelta(3.928571429, solution(0).x) && withinDelta(1, solution(0).y));
     QVERIFY(withinDelta(1.375, solution(1).x) && withinDelta(3.5, solution(1).y));
     QVERIFY(withinDelta(5.071428571, solution(2).x) && withinDelta(6, solution(2).y));
}

QTEST_MAIN(LinearFunctionsAndLogicalImageTests);
