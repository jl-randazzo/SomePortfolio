#ifndef ANIMFRAMEOBJECT_H
#define ANIMFRAMEOBJECT_H

#include <QBasics.h>
#include <OpenGLSessionManager.h>
#include <QOpenGLTexture>
#include <logicalimage.h>
#include <QtOpenGL>
#include <gl/GL.h>
#include <gl/GLU.h>

enum DisplayMode { Sprite, GrayScale, Lines, Eroded, Edges, Depop, Candidates };
enum ImgObjType { FirstFrame, SubsequentFrame, NonFrame };

//delegate class
class AnimFrameObjectDelegate : public QAbstractItemDelegate {
public:
    AnimFrameObjectDelegate(QObject *parent);
    void paint(QPainter *painter, const QStyleOptionViewItem &option, const QModelIndex &index) const override;
    QSize sizeHint(const QStyleOptionViewItem &option, const QModelIndex &index) const override;
};

class AnimFrameObject {
friend class AnimationSessionBuilder;
public:
    AnimFrameObject(const QString name, const QImage *sprite, const ImgObjType type = NonFrame );
    void pushToVertexBuffer(QSize displaySize, double magnification, DisplayMode mode) const;
    QString getName() const;
    QImage getThumbnail() const;

private:
    ~AnimFrameObject();
    void setupGrayscaleProgram() const;
    void setupStandardProgram(const QOpenGLTexture* tex) const;
    void pushFrameToVertexBuffer(QSize displaySize, double magnification, DisplayMode mode) const;
    void pushRectToVertexBuffer(QSize tex_size, QSize displaySize, double magnification) const;
    void generateGrayScale();

    // instance variables
    const QString name;
    const QImage *sprite;
    QImage thumbnail;
    ImgObjType type;
    QOpenGLTexture const* texture;
    LogicalImage* logical_image;

    // processing variables
    QImage* gray;
    QImage* gray_thresholded;
    QImage* eroded;
    QImage* edge_detected;
    QImage* depop;
    QImage* curve_candidates;

    // textures
    QOpenGLTexture const* gray_text;
    QOpenGLTexture const* gray_thresh_text;
    QOpenGLTexture const* eroded_text;
    QOpenGLTexture const* edge_text;
    QOpenGLTexture const* depop_text;
    QOpenGLTexture const* curve_candidates_text;

};

#endif // ANIMFRAMEOBJECT_H
