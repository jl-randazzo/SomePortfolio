#ifndef PRIMARYFRAME_H
#define PRIMARYFRAME_H

#include <framelistview.h>

#include <QWidget>
#include <QOpenGLWidget>
#include <QOpenGLFunctions>
#include <QBasics.h>
#include <QDropEvent>
#include <QDragEnterEvent>
#include <QMimeData>
#include <gl/GLU.h>
#include <QtOpenGL>
#include "animationsessionbuilder.h"

class PrimaryFrame : public QOpenGLWidget
{
public:
    PrimaryFrame(QWidget *parent);
    void paintGL() override;
    void setDisplayMode(DisplayMode mode);
    DisplayMode getDisplayMode() const;

private:
    void initializeGL() override;
    void dragEnterEvent(QDragEnterEvent *event) override;
    void dropEvent(QDropEvent *event) override;

    // instance variables
    DisplayMode mode = Sprite;
    AnimationSessionBuilder *animSession() { return AnimationSessionBuilder::getInstance(); }
    QPainter *painter;
    QOpenGLFunctions *glFunc;
};

#endif // PRIMARYFRAME_H
