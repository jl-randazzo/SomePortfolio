#include<imageprocessing.h>

void squashAlphaToBlack(QImage& image){
    for(int m = 0; m < image.height(); m++){
        QRgb* scanline = (QRgb*)image.scanLine(m);
        for(int n = 0; n < image.width(); n++){
            if(scanline[n] >> 24 == 0){
               scanline[n] = 0x00000000;
            }
        }
    }
}

errno_t extractGrayScaleAndLines(QImage original, QImage **grayscale_container, QImage **line_image_container){
    *grayscale_container = new QImage(original);
    squashAlphaToBlack(**grayscale_container);
    (*grayscale_container)->convertTo(QImage::Format::Format_Grayscale8);
    *line_image_container = new QImage(inverseThresholdOnGrayScale(**grayscale_container, 80));
    return 0;
}

QImage inverseThresholdOnGrayScale(QImage grayscale, int value){
    assert(grayscale.format() == QImage::Format::Format_Grayscale8);

    QImage new_image = QImage(grayscale);

    for(int y = 0; y < grayscale.height(); y++){
        uint8_t * orig_scan = (uint8_t*)grayscale.scanLine(y);
        uint8_t * new_scan = (uint8_t*)new_image.scanLine(y);
        for(int x = 0; x < grayscale.width(); x++){
            if(orig_scan[x] > value){
                new_scan[x] = 0xff;
            } else {
                new_scan[x] = 0x00;
            }
        }
    }

    return new_image;
}

QImage eradicatePattern(QImage original, QGenericMatrix<3, 3, bool> pattern){
    QImage eradicate(original);

    auto data_func = [=](QGenericMatrix<3, 3, uint8_t> mat) -> uint8_t {
        bool acc = matchesPattern<3>(mat, pattern);
        return acc ? 0 : mat(1, 1);
    };

    abstractMorphologicalProcess<3, uint8_t, QImage>(&original, &eradicate, data_func);
    return eradicate;
}

QImage depopulateNeighborhoods(QImage original){
    QImage duplicate(original);

    bool pattern_a[]
    {0, 1, 0,
    1, 1, 1,
    0, 1, 0};
    QGenericMatrix<3, 3, bool> pat_mat(pattern_a);

    duplicate = eradicatePattern(original, pat_mat);

    bool pattern_b[]
    {0, 1, 0,
    1, 1, 0,
    0, 0, 0};
    pat_mat = QGenericMatrix<3, 3, bool>(pattern_b);

    return eradicatePattern(duplicate, pat_mat);
}


QImage removeOrphans(QImage original){
    QImage duplicate(original);

    auto data_func = [=](QGenericMatrix<3, 3, uint8_t> mat) -> uint8_t {
        int neighbor_count = 0;

        for(int i = 0; i < 3; i++){
            for(int j = 0; j < 3; j++){
                neighbor_count += mat(i, j) ? 1 : 0;
            }
        }

        return neighbor_count > 2 ? mat(1,1) : 0;
    };

    abstractMorphologicalProcess<3, uint8_t, QImage>(&original, &duplicate, data_func);
    return duplicate;
}

QImage applySobelFilter(QImage original){
    double x_data[]{0, -1, 0,
                   0,  0,  0,
                   0,  1,  0};
    QGenericMatrix<3, 3, double> sobel_x(x_data);

    double y_data[]{0, 0, 0
                    -1, 0, 1,
                    0, 0, 0};
    QGenericMatrix<3, 3, double> sobel_y(y_data);

    QImage x_img = grayscaleConvolution<3>(original, sobel_x);
    QImage y_img = grayscaleConvolution<3>(original, sobel_y);

    return abstractImageCombination<uint8_t>(x_img, y_img, [](uint8_t a, uint8_t b) -> uint8_t {
        qreal c_sq = qPow(a, 2) + qPow(b, 2);
        return static_cast<uint8_t>(qSqrt(c_sq));
    });
}

void fillGaps(const QImage* compare, QImage* change, std::vector<QGenericMatrix<3, 3, bool>> gap_patterns){
    bool empty[] {0, 0, 0,
                  0, 0, 0,
                  0, 0, 0 };
    auto empty_pat = QGenericMatrix<3, 3, bool>(empty);

    auto data_func = [=](QGenericMatrix<3, 3, uint8_t> mat) -> uint8_t {
        auto it = gap_patterns.begin();
        bool found = false;

        if(!mat(1,1) && !matchesPattern(mat, empty_pat, true)){
            while(it != gap_patterns.end() && !found){
                found |= matchesPattern(mat, *it, true);
                it++;
            }

            return found ? 255 : 0;
        }
        return mat(1,1);
    };

    abstractMorphologicalProcess<3, uint8_t, QImage>(compare, change, data_func);
}

QImage fillMajorGaps(const QImage original){
    QImage duplicate(original);

    std::vector<QGenericMatrix<3, 3, bool>> gap_patterns;
    bool pat_a[] {0, 1, 0,
                  0, 0, 0,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_a));
    bool pat_b[] {0, 1, 0,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_b));
    bool pat_c[] {0, 1, 0,
                  0, 0, 0,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_c));
    bool pat_e[] {1, 0, 0,
                  0, 0, 1,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_e));
    bool pat_f[] {0, 0, 0,
                  1, 0, 1,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_f));
    bool pat_g[] {0, 0, 1,
                  1, 0, 0,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_g));
    bool pat_i[] {0, 0, 1,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_i));
    bool pat_j[] {1, 0, 0,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_j));
    bool pat_k[] {1, 1, 0,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_k));
    bool pat_l[] {1, 1, 1,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_l));
    bool pat_m[] {0, 1, 1,
                  0, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_m));
    bool pat_n[] {0, 0, 1,
                  1, 0, 1,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_n));
    bool pat_o[] {0, 0, 1,
                  1, 0, 1,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_o));
    bool pat_p[] {0, 0, 0,
                  1, 0, 1,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_p));
    bool pat_q[] {0, 1, 0,
                  0, 0, 0,
                  0, 1, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_q));
    bool pat_r[] {0, 1, 0,
                  0, 0, 0,
                  1, 1, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_r));
    bool pat_s[] {0, 1, 0,
                  0, 0, 0,
                  1, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_s));
    bool pat_t[] {0, 0, 0,
                  1, 0, 1,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_t));
    bool pat_u[] {1, 0, 0,
                  1, 0, 1,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_u));
    bool pat_v[] {1, 0, 0,
                  1, 0, 1,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_v));
    bool pat_w[] {1, 0, 0,
                  0, 0, 0,
                  1, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_w));
    bool pat_x[] {0, 0, 1,
                  0, 0, 0,
                  0, 1, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_x));
    bool pat_y[] {0, 0, 0,
                  0, 0, 1,
                  1, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_y));
    bool pat_z[] {1, 0, 1,
                  0, 0, 1,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_z));
    bool pat_aa[] {1, 1, 0,
                  0, 0, 0,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_aa));
    bool pat_ab[] {0, 1, 1,
                  0, 0, 0,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_ab));
    bool pat_ac[] {1, 0, 1,
                  1, 0, 0,
                  0, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_ac));
    bool pat_ad[] {0, 0, 0,
                  1, 0, 0,
                  1, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_ad));

    fillGaps(&original, &duplicate, gap_patterns);
    return duplicate;
}

QImage fillDiagonalGaps(const QImage original){
    QImage duplicate(original);

    std::vector<QGenericMatrix<3, 3, bool>> gap_patterns;
    bool pat_a[] {1, 0, 0,
                  0, 0, 0,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_a));
    bool pat_b[] {0, 0, 1,
                  0, 0, 0,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_b));
    bool pat_d[] {0, 1, 0,
                  0, 0, 1,
                  1, 0, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_d));
    bool pat_w[] {1, 0, 0,
                  0, 0, 1,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_w));
    bool pat_x[] {0, 0, 1,
                  1, 0, 0,
                  0, 1, 0 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_x));
    bool pat_h[] {0, 1, 0,
                  1, 0, 0,
                  0, 0, 1 };
    gap_patterns.push_back(QGenericMatrix<3, 3, bool>(pat_h));

    fillGaps(&duplicate, &duplicate, gap_patterns);
    return duplicate;
}

/*
 * This is an incomplete attempt at generic convolution of an image
 * Grayscale is all I need for now, so it receives my focus
 */
//template <int M, class T>
//QImage convolution(QImage original, QGenericMatrix<M, M, double> kernel){
//    assert((M % 2) == 1);
//    int center = M / 2;

//    qInfo() << "size of type 'T': " << sizeof(T);

//    QImage conv(original);

//    T* scan_lines = new T*[M];

//    for(int i = center; i < conv.height() - center; i++){

//        T* new_line = conv.scanLine(i);

//        // extract scan lines
//        for(int m = 1; m <= center; m++){
//            scan_lines[center - m] = (T*)original.scanLine(i - m);
//            scan_lines[center + m] = (T*)original.scanLine(i + m);
//        }
//        scan_lines[center] = static_cast<T*>(original.scanLine(i));

//        for(int j = center; j < conv.width() - center; j++){

//            // convolute
//            double sum = 0;
//            for(int m = 1; m <= center; m++){
//                for(int n = 1; n <= center; n++){
//                    // along each path of the diagonal
//                    sum += kernel[center - m][center - n] * scan_lines[center - m][j - n];
//                    sum += kernel[center - m][center + n] * scan_lines[center - m][j + n];
//                    sum += kernel[center + m][center + n] * scan_lines[center + m][j + n];
//                    sum += kernel[center + m][center - n] * scan_lines[center + m][j - n];
//                }

//                // running along the center
//                sum += kernel[center][center - m] * scan_lines[center][j - m];
//                sum += kernel[center][center + m] * scan_lines[center][j + m];
//                sum += kernel[center - m][center] * scan_lines[center - m][j];
//                sum += kernel[center + m][center] * scan_lines[center + m][j];
//            }
//            sum += kernel[center][center] * scan_lines[center][j];

//            new_line[j] = static_cast<T>(static_cast<uint>(sum));
//        }
//    }

//    delete[] scan_lines;
//}
