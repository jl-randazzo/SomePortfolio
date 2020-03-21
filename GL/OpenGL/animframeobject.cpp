#include "animframeobject.h"
#include<QPixmap>
#include<QFlags>
#include<imageprocessing.h>

// AnimFrameObjectDelegate

AnimFrameObjectDelegate::AnimFrameObjectDelegate(QObject * parent) : QAbstractItemDelegate(parent){}


void AnimFrameObjectDelegate::paint(QPainter *painter, const QStyleOptionViewItem &option, const QModelIndex &index) const {

    if(option.state & QStyle::State_HasFocus){
        painter->fillRect(option.rect, QColor(200, 220, 220, 255));
    }

    QVariant data = index.data(Qt::DecorationRole);
    QImage frame = data.value<QImage>();
    painter->drawImage(option.rect, frame, frame.rect());

    if (option.state & QStyle::State_MouseOver) {
        QRect rect = option.rect;
        rect.setWidth(rect.width() - 1);
        rect.setHeight(rect.height() - 5);
        painter->setPen(QColor(60, 100, 100, 255));
        painter->drawRect(rect);
    }
}

QSize AnimFrameObjectDelegate::sizeHint(const QStyleOptionViewItem &option, const QModelIndex &index) const {
    return QSize(100,100);
}

// AnimFrameObject

AnimFrameObject::AnimFrameObject(const QString name, const QImage *sprite, ImgObjType type)
    : name(name), sprite(sprite), type(type) {
    thumbnail = sprite->scaled(100, 100, Qt::KeepAspectRatio, Qt::TransformationMode::SmoothTransformation);

    texture = OpenGLSessionManager::getInstance()->createOpenGLTexture(sprite);
    qInfo() << "Tex Id: " << texture->textureId() << " Text target: " << texture->target();
}

AnimFrameObject::~AnimFrameObject(){
    delete texture;
    delete sprite;
    //delete logical_image;
    delete gray;
    delete gray_thresholded;
    delete eroded;
    delete edge_detected;
    delete depop;
    delete gray_text;
    delete gray_thresh_text;
    delete eroded_text;
    delete edge_text;
    delete depop_text;
}

QString AnimFrameObject::getName() const {
    return name;
}

QImage AnimFrameObject::getThumbnail() const {
    return thumbnail;
}

void AnimFrameObject::generateGrayScale() {
    extractGrayScaleAndLines(*sprite, &gray, &gray_thresholded);

    bool data[] {false, false, false,
                 false, true, true,
                 false, false, false };
    QGenericMatrix<3, 3, bool> structuring_element(data);

    eroded = new QImage(grayscaleLineErosion<3>(*gray_thresholded, structuring_element));

    edge_detected = new QImage(applySobelFilter(*eroded));
    QString save_path = QDir::currentPath() + "\\" + name + "_edge.png";
    qInfo() << save_path << " attempt : " << edge_detected->save(save_path, nullptr, 50);

    QImage depop_a = depopulateNeighborhoods(*edge_detected);
    QImage gap_fill = fillMajorGaps(depop_a);
    QImage orphan_removal = removeOrphans(gap_fill);
    depop = new QImage(fillDiagonalGaps(orphan_removal));
    save_path = QDir::currentPath() + "\\" +  name + "_depop.png";
    qInfo() << save_path << " attempt : " << depop->save(save_path, nullptr, 50);
    logical_image = new LogicalImage(depop);
    curve_candidates = logical_image->writeColorsToNewImage();

    gray_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(gray);
    gray_thresh_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(gray_thresholded);
    eroded_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(eroded);
    edge_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(edge_detected);
    depop_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(depop);
    curve_candidates_text = OpenGLSessionManager::getInstance()->createOpenGLTexture(curve_candidates);
}

void AnimFrameObject::pushToVertexBuffer(QSize displaySize, double magnification = 1, DisplayMode mode = Sprite) const {
    switch(type){
    case NonFrame:
        setupStandardProgram(texture);
        pushRectToVertexBuffer(sprite->size(), displaySize, magnification);
        break;
    case FirstFrame:
        pushFrameToVertexBuffer(displaySize, magnification, mode);
        break;
    case SubsequentFrame:
        pushFrameToVertexBuffer(displaySize, magnification, mode);
        break;
    }
}

void AnimFrameObject::pushFrameToVertexBuffer(QSize displaySize, double magnification, DisplayMode mode) const {
    switch(mode){
        case Sprite:
        setupStandardProgram(texture);
        break;
    case GrayScale:
        setupGrayscaleProgram();
        break;
    case Lines:
        setupStandardProgram(gray_thresh_text);
        break;
    case Edges:
        setupStandardProgram(edge_text);
        break;
    case Eroded:
        setupStandardProgram(eroded_text);
        break;
    case Depop:
        setupStandardProgram(depop_text);
        break;
    case Candidates:
        setupStandardProgram(curve_candidates_text);
        break;
    }

    pushRectToVertexBuffer(sprite->size(), displaySize, magnification);
}

void AnimFrameObject::pushRectToVertexBuffer(QSize tex_size, QSize displaySize, double magnification = 1) const {

    GLfloat halfWidth = (double)displaySize.width() / (double)2;
    GLfloat halfHeight = (double)displaySize.height() / (double)2;
    GLfloat ssxCoord = ((double)tex_size.width() * magnification) / (double)2 / halfWidth;
    GLfloat ssyCoord = ((double)tex_size.height() * magnification) / (double)2 / halfHeight;

    GLfloat vertex_data[] {
        -ssxCoord, -ssyCoord, 0, // vertex
                0, 0, // tex_coords
        -ssxCoord, ssyCoord, 0,
                0, 1,
        ssxCoord, ssyCoord, 0,
                1, 1,
        ssxCoord, -ssyCoord, 0,
                1, 0
    };

    int sizes[] {3, 2};
    GLenum types[] {GL_FLOAT, GL_FLOAT};
    OpenGLSessionManager::getInstance()->
            configureAttributesAndDraw(GL_QUADS, 2, sizes, types, 4, vertex_data);
}

void AnimFrameObject::setupStandardProgram(const QOpenGLTexture* tex) const {
    const QOpenGLContext* context = OpenGLSessionManager::getInstance()->getContext();

    GLuint prog_id = OpenGLSessionManager::getInstance()->useProgram("Default");

    OpenGLSessionManager::getInstance()->setUniform2DTextureValue(prog_id, "sprite", tex->textureId(), 0, GL_TEXTURE0);

    context->functions()->glEnable(GL_BLEND);
    context->functions()->glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
}

void AnimFrameObject::setupGrayscaleProgram() const {
    GLuint prog_id = OpenGLSessionManager::getInstance()->useProgram("Grayscale");

    OpenGLSessionManager::getInstance()->setUniform2DTextureValue(prog_id, "gray_scale", gray_text->textureId(), 0, GL_TEXTURE0);
    OpenGLSessionManager::getInstance()->setUniform2DTextureValue(prog_id, "sprite", texture->textureId(), 1, GL_TEXTURE1);

    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
}
