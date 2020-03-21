#ifndef IMAGEPROCESSING_H
#define IMAGEPROCESSING_H
#include <Qbasics.h>
#include <QgenericMatrix>
#include<QtMath>
QImage inverseThresholdOnGrayScale(QImage grayscale, int value);
errno_t extractGrayScaleAndLines(QImage original, QImage **grayscale_container, QImage **line_image_container);

template <int M>
QImage grayscaleConvolution(QImage original, QGenericMatrix<M, M, double> kernel, bool take_abs = true){
    assert((M % 2) == 1);
    int center = M / 2;

    QImage conv(original);

    uint8_t** scan_lines = new uint8_t*[M];

    for(int i = center; i < conv.height() - center; i++){

        uint8_t* new_line = conv.scanLine(i);

        // extract scan lines
        for(int m = 1; m <= center; m++){
            scan_lines[center - m] = static_cast<uint8_t*>(original.scanLine(i - m));
            scan_lines[center + m] = static_cast<uint8_t*>(original.scanLine(i + m));
        }
        scan_lines[center] = static_cast<uint8_t*>(original.scanLine(i));

        for(int j = center; j < conv.width() - center; j++){

            // convolute
            double sum = 0;
            for(int m = 1; m <= center; m++){
                for(int n = 1; n <= center; n++){
                    // along each path of the diagonal
                    sum += kernel(center - m, center - n) * scan_lines[center - m][j - n];
                    sum += kernel(center - m, center + n) * scan_lines[center - m][j + n];
                    sum += kernel(center + m, center + n) * scan_lines[center + m][j + n];
                    sum += kernel(center + m, center - n) * scan_lines[center + m][j - n];
                }

                // running along the center
                sum += kernel(center, center - m) * scan_lines[center][j - m];
                sum += kernel(center, center + m) * scan_lines[center][j + m];
                sum += kernel(center - m, center) * scan_lines[center - m][j];
                sum += kernel(center + m, center) * scan_lines[center + m][j];
            }
            sum += kernel(center, center) * scan_lines[center][j];

            if(take_abs){
                sum = qAbs(sum);
            }

            new_line[j] = static_cast<uint8_t>(sum);
        }
    }

    delete[] scan_lines;

    return conv;
}

/*
 * perElT designates the analysis results for each element in structuring function
 * Previous results are passed in after each operation, and the value will ostensibly change
 * for each analysis.
 *
 * After the final analysis, the result from the structuring function is passed as an argument
 * to the result_function, which returns the resT type, equivalent to the data type used to
 * store pixel data in the resulting QImage
 *
 * d_val is the default value at the start of every structuring operation. It can be a pointer to a list,
 * a true/false value for bools, or anything that fits the design of the structuring system.
 */
template<int M, class perElT, class resT>
void abstractMorphologicalProcess(QImage* original, QImage* morph, QGenericMatrix<M, M, bool> struct_el, perElT d_val,
                                    std::function<perElT(perElT, bool, resT)> structuring_function,
                                    std::function<resT(perElT)> result_function){
    assert((M % 2) == 1);
    assert(original->size() == morph->size());

    int center = M / 2;

    resT** scan_lines = new resT*[M];

    for(int i = center; i < morph->height() - center; i++){

        resT* new_line = morph->scanLine(i);

        // extract scan lines
        for(int m = 1; m <= center; m++){
            scan_lines[center - m] = static_cast<resT*>(original->scanLine(i - m));
            scan_lines[center + m] = static_cast<resT*>(original->scanLine(i + m));
        }
        scan_lines[center] = static_cast<resT*>(original->scanLine(i));

        for(int j = center; j < morph->width() - center; j++){

            // structure
            perElT structuring_state = d_val;
            for(int m = 1; m <= center; m++){
                for(int n = 1; n <= center; n++){
                    // along each path of the diagonal
                    structuring_state = structuring_function(structuring_state, struct_el(center - m, center - n), scan_lines[center - m][j - n]);
                    structuring_state = structuring_function(structuring_state, struct_el(center - m, center + n), scan_lines[center - m][j + n]);
                    structuring_state = structuring_function(structuring_state, struct_el(center + m, center + n), scan_lines[center + m][j + n]);
                    structuring_state = structuring_function(structuring_state, struct_el(center + m, center - n), scan_lines[center + m][j - n]);
                }

                // running along the center
                structuring_state = structuring_function(structuring_state, struct_el(center, center - m), scan_lines[center][j - m]);
                structuring_state = structuring_function(structuring_state, struct_el(center, center + m), scan_lines[center][j + m]);
                structuring_state = structuring_function(structuring_state, struct_el(center - m, center), scan_lines[center - m][j]);
                structuring_state = structuring_function(structuring_state, struct_el(center + m, center), scan_lines[center + m][j]);
            }
            structuring_state = structuring_function(structuring_state, struct_el(center, center), scan_lines[center][j]);

            new_line[j] = result_function(structuring_state);
        }
    }

    delete[] scan_lines;
}

/*
 * This version delivers the MxM matrix of resT elements at each point throughout the TImage
 * to the caller's data_func. It is the responsibility of the caller's data_func to use those data
 * to decide the value of the corresponding pixel in the target image (morph)
 */

template<int M, class resT, class TImage>
void abstractMorphologicalProcess(const TImage* original, TImage* morph,
                                  std::function<resT(QGenericMatrix<M,M,resT>)> data_func){
    assert((M % 2) == 1);
    assert(original->size() == morph->size());

    int center = M / 2;

    const resT** scan_lines = new const resT*[M];

    for(int i = center; i < morph->height() - center; i++){

        resT* new_line = morph->scanLine(i);

        // extract scan lines
        for(int m = 1; m <= center; m++){
            scan_lines[center - m] = static_cast<const resT*>(original->constScanLine(i - m));
            scan_lines[center + m] = static_cast<const resT*>(original->constScanLine(i + m));
        }
        scan_lines[center] = static_cast<const resT*>(original->constScanLine(i));

        for(int j = center; j < morph->width() - center; j++){

            // structure
            QGenericMatrix<M,M,resT> window;
            for(int m = 1; m <= center; m++){
                for(int n = 1; n <= center; n++){
                    if(scan_lines[center][j] > 0){
                        QString string("blah");
                    }
                    // along each path of the diagonal
                    window(center - m, center - n) = scan_lines[center - m][j - n];
                    window(center - m, center + n) = scan_lines[center - m][j + n];
                    window(center + m, center + n) = scan_lines[center + m][j + n];
                    window(center + m, center - n) = scan_lines[center + m][j - n];
                }

                // running along the center
                window(center, center - m) = scan_lines[center][j - m];
                window(center, center + m) = scan_lines[center][j + m];
                window(center - m, center) = scan_lines[center - m][j];
                window(center + m, center) = scan_lines[center + m][j];
            }
            window(center, center) = scan_lines[center][j];

            new_line[j] = data_func(window);
        }
    }

    delete[] scan_lines;
}

template<class T>
QImage abstractImageCombination(QImage a, QImage b, std::function<T(T, T)> combination_func){
    QImage c(a);
    assert(a.size() == b.size());

    for(int m = 0; m < a.height(); m++){
        T* scanline_a = static_cast<T*>(a.scanLine(m));
        T* scanline_b = static_cast<T*>(b.scanLine(m));
        T* scanline_c = static_cast<T*>(c.scanLine(m));
        for(int n = 0; n < a.width(); n++){
            scanline_c[n] = combination_func(scanline_a[n], scanline_b[n]);
        }
    }

    return c;
}

template<int M>
bool matchesPattern(QGenericMatrix<M, M, uint8_t> im_data, QGenericMatrix<M, M, bool> pattern, bool exclusive = false){
    bool acc = true;

    for(int i = 0; i < 3; i++){
        for(int j = 0; j < 3; j++){
            acc &= (pattern(i, j) && im_data(i, j)) || (!exclusive ? (!pattern(i, j)) : (!pattern(i,j) && !im_data(i, j)));
            if(!acc)
                break;
        }
        if(!acc)
            break;
    }

    return acc;
}

template<int M>
QImage grayscaleLineErosion(QImage original, QGenericMatrix<M, M, bool> structuring_element) {
    QImage morph(original);

    auto structuring_function = [](bool accumulator, bool struct_in, uint8_t im_in) -> bool {
        if(struct_in){
            return accumulator & !im_in;
        } else {
            return accumulator;
        }
    };

    auto result_function = [](bool result) -> uint8_t {
        return result ? 0 : 255;
    };

    abstractMorphologicalProcess<M, bool, uint8_t>(&original, &morph, structuring_element, true, structuring_function, result_function);
    return morph;
}

// implemented in source file

QImage applySobelFilter(QImage original);
void fillGaps(const QImage* compare, QImage* change, std::vector<QGenericMatrix<3, 3, bool>> gap_patterns);
QImage fillMajorGaps(const QImage original);
QImage fillDiagonalGaps(const QImage original);
QImage removeOrphans(QImage original);
QImage depopulateNeighborhoods(QImage original);

#endif // IMAGEPROCESSING_H
