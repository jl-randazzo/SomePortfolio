#include "primaryframe.h"

PrimaryFrame::PrimaryFrame(QWidget *parent): QOpenGLWidget(parent){
    painter = new QPainter();
}

void PrimaryFrame::initializeGL(){
    QSurfaceFormat format;
    format.setProfile(QSurfaceFormat::CompatibilityProfile);
    format.setVersion(4,6);
    context()->setFormat(format);
    qInfo() << "context created" << context()->create();
    makeCurrent();

    QOpenGLVertexArrayObject vao(this);
    qInfo() << "vao created" << vao.create();
    vao.bind();

    GLuint framebuffer = 0;
    context()->functions()->glGenFramebuffers(1, &framebuffer);
    context()->functions()->glBindFramebuffer(GL_FRAMEBUFFER, framebuffer);

    OpenGLSessionManager::buildSession(context());
    animSession()->loadSystemImages();
    glClearColor(.4f, .4f, .4f, 1);
}

void PrimaryFrame::paintGL(){
    glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
    glClear(GL_COLOR_BUFFER_BIT);
    if(animSession()->activeSession()){
        animSession()->getImage(FrameListView::getInstance()->getSelection()->getName())->pushToVertexBuffer(this->size(), 1, mode);
    } else {
        animSession()->getImage("dragAndDropIcon")->pushToVertexBuffer(this->size(), .5, mode);
    }
    //glFlush();
    //update();
    return;
}

DisplayMode PrimaryFrame::getDisplayMode() const{
    return mode;
}

void PrimaryFrame::setDisplayMode(DisplayMode mode){
    this->mode = mode;
}

void PrimaryFrame::dropEvent(QDropEvent *event){
    event->acceptProposedAction();
    if(event->mimeData()->hasUrls()){
        for(QUrl url : event->mimeData()->urls()){
            qInfo() << url.url();
        }
    } else {
        return; // error, not urls
    }

    errno_t error = animSession()->loadFrames(event->mimeData()->urls());
}


void PrimaryFrame::dragEnterEvent(QDragEnterEvent *event){
    event->acceptProposedAction();
    qInfo() << "drag enter";
}
